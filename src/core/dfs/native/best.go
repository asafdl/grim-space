package main

/*
#include "bridge.h"
*/
import "C"

import (
	"fmt"
	"math"
	"sort"
	"sync"
	"sync/atomic"
	"time"
	"unsafe"
)

type boundedNode struct {
	searchFrame
	score, bound int64
}

type boundedState struct {
	sync.Mutex
	ready      *sync.Cond
	queue      []boundedNode
	pending    int
	queueLimit int
	best       atomic.Int64
	path       []C.int32_t
	evaluated  atomic.Int64
	dominated  atomic.Int64
	pruned     atomic.Int64
	failed     atomic.Bool
	deadline   time.Time
	exhausted  atomic.Bool
}

func (b *boundedState) stopped() bool {
	if b.failed.Load() || b.exhausted.Load() {
		return true
	}
	if !b.deadline.IsZero() && !time.Now().Before(b.deadline) {
		b.exhausted.Store(true)
		return true
	}
	return false
}

func (b *boundedState) improve(node boundedNode) {
	if node.score == math.MinInt32 || node.score <= b.best.Load() {
		return
	}
	b.Lock()
	defer b.Unlock()
	if node.score > b.best.Load() {
		b.path = append([]C.int32_t{}, node.path...)
		b.best.Store(node.score)
	}
}

func (b *boundedState) enqueue(node boundedNode) bool {
	b.Lock()
	defer b.Unlock()
	if len(b.queue) >= b.queueLimit || b.stopped() {
		return false
	}
	b.queue = append(b.queue, node)
	b.pending++
	b.ready.Signal()
	return true
}

func (b *boundedState) take() (boundedNode, bool) {
	b.Lock()
	defer b.Unlock()
	for len(b.queue) == 0 && b.pending != 0 {
		if b.stopped() {
			return boundedNode{}, false
		}
		b.ready.Wait()
	}
	if len(b.queue) == 0 || b.stopped() {
		return boundedNode{}, false
	}
	last := len(b.queue) - 1
	node := b.queue[last]
	b.queue[last] = boundedNode{}
	b.queue = b.queue[:last]
	return node, true
}

func (b *boundedState) complete() {
	b.Lock()
	defer b.Unlock()
	b.pending--
	b.ready.Broadcast()
}

// Evaluate on the worker's existing simulation, without copying a world or retaining a result frame.
func (s *actionSearch) inspectBest(path []C.int32_t, b *boundedState) (boundedNode, bool) {
	// Always score the root so even an exhausted budget can return a legal incumbent.
	if len(path) > 0 && b.stopped() {
		return boundedNode{}, false
	}
	if s.query(path, 5, 0) != 2 {
		if s.err == nil {
			s.err = fmt.Errorf("missing score and upper bound")
		}
		b.failed.Store(true)
		return boundedNode{}, false
	}
	b.evaluated.Add(1)
	node := boundedNode{
		searchFrame: snapshot(path, phaseRemaining),
		score:       int64(s.buffer[0]), bound: int64(s.buffer[1]),
	}
	b.improve(node)
	if node.bound == math.MinInt32 || node.bound <= b.best.Load() {
		b.pruned.Add(1)
		return boundedNode{}, false
	}
	if b.stopped() {
		return boundedNode{}, false
	}
	_, dominated := s.visitNode(path, C.int32_t(node.score))
	if s.err != nil {
		b.failed.Store(true)
		return boundedNode{}, false
	}
	if dominated {
		b.dominated.Add(1)
		return boundedNode{}, false
	}
	return node, true
}

func (s *actionSearch) expandBest(node boundedNode, b *boundedState) {
	if b.stopped() || len(node.path) >= maxSearchDepth {
		return
	}
	if node.bound <= b.best.Load() {
		b.pruned.Add(1)
		return
	}
	var children []boundedNode
	for family := 0; family < s.families; family++ {
		if b.stopped() {
			return
		}
		count := s.query(node.path, discover, family)
		if count < 0 {
			b.failed.Store(true)
			return
		}
		candidates := append([]C.int32_t(nil), s.buffer[:count]...)
		for _, candidate := range candidates {
			if b.stopped() {
				return
			}
			legal := s.query(node.path, apply, int(candidate))
			if legal < 0 {
				b.failed.Store(true)
				return
			}
			if legal == 0 {
				continue
			}
			path := append(append([]C.int32_t(nil), node.path...), candidate)
			if child, ok := s.inspectBest(path, b); ok {
				children = append(children, child)
			}
		}
	}
	sort.SliceStable(children, func(i, j int) bool {
		if children[i].score != children[j].score {
			return children[i].score > children[j].score
		}
		return children[i].bound > children[j].bound
	})
	for index, child := range children {
		if b.stopped() {
			return
		}
		if child.bound <= b.best.Load() {
			b.pruned.Add(1)
			continue
		}
		// Finish the promising child before making its siblings available to other workers.
		if index == 0 || !b.enqueue(child) {
			s.expandBest(child, b)
		}
	}
}

//export bridge_search_best
func bridge_search_best(
	familyCount, capacity, workers C.int32_t, rules C.bridge_search_rules,
	budgetNanoseconds C.int64_t,
	handle *C.uint64_t, statistics *C.int64_t,
) C.int32_t {
	if familyCount <= 0 || capacity < 2 || workers < 1 || workers > 64 || rules == nil || budgetNanoseconds < 0 || handle == nil || statistics == nil {
		return failed
	}
	b := &boundedState{queueLimit: int(workers) * 4}
	if budgetNanoseconds > 0 {
		b.deadline = time.Now().Add(time.Duration(budgetNanoseconds))
	}
	b.ready = sync.NewCond(&b.Mutex)
	b.best.Store(math.MinInt64)
	root := &actionSearch{
		rules: rules, families: int(familyCount),
		buffer:   make([]C.int32_t, int(capacity)),
		frontier: &visitFrontier{visited: make(map[C.int32_t][][]C.int32_t)},
	}
	if node, ok := root.inspectBest(nil, b); ok {
		b.enqueue(node)
	}
	var done sync.WaitGroup
	for index := 0; index < int(workers); index++ {
		worker := root.newWorker(index + 1)
		done.Add(1)
		go func() {
			defer done.Done()
			for {
				node, ok := b.take()
				if !ok {
					return
				}
				worker.expandBest(node, b)
				b.complete()
			}
		}()
	}
	done.Wait()
	if b.failed.Load() {
		return failed
	}
	score := b.best.Load()
	frames := []searchFrame{}
	if score != math.MinInt64 {
		frames = append(frames, snapshot(b.path, phaseRemaining))
	} else {
		score = math.MinInt32
	}
	stats := unsafe.Slice(statistics, 5)
	stats[0], stats[1], stats[2], stats[3] = C.int64_t(b.evaluated.Load()),
		C.int64_t(b.dominated.Load()), C.int64_t(b.pruned.Load()), C.int64_t(score)
	if b.exhausted.Load() {
		stats[4] = 1
	}
	sessions.Lock()
	defer sessions.Unlock()
	sessions.next++
	sessions.all[sessions.next] = &session{frames: frames}
	*handle = C.uint64_t(sessions.next)
	return accepted
}

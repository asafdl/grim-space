package main

/*
#include "bridge.h"
*/
import "C"

import (
	"fmt"
	"sync"
	"unsafe"
)

// Callback operations. Go never sees world state: a node is the path of opaque action handles
// from the search root, and C# moves its per-worker simulation to that path before answering.
const (
	discover = iota // argument: action family; output: candidate handles
	apply           // argument: candidate handle; returns 1 when legal (the C# cursor is then at the child)
	visit           // output: [yield frame, interned visit key, budget...]
	decide          // argument: phase; returns Continue, PruneChildren or Stop
	priority        // returns 1 when the path stays in the priority phase
)

const (
	phasePriority  = 0
	phaseRemaining = 1
	maxSearchDepth = 20
	hardAbortDepth = 64
)

type searchFrame struct {
	path  []C.int32_t
	phase C.int32_t
}

type actionSearch struct {
	worker   int
	rules    C.bridge_search_rules
	families int
	buffer   []C.int32_t
	frontier *visitFrontier
	frames   []searchFrame
	deferred []searchFrame
	tasks    []searchFrame
	split    bool
	stopped  bool
	err      error
}

type visitFrontier struct {
	sync.Mutex
	visited map[C.int32_t][][]C.int32_t
}

func pointer(values []C.int32_t) *C.int32_t {
	if len(values) == 0 {
		return nil
	}
	return &values[0]
}

func (s *actionSearch) query(path []C.int32_t, operation, argument int) int {
	if s.err != nil {
		return -1
	}
	n := int(C.bridge_query(s.rules, C.int32_t(s.worker), C.int32_t(operation), C.int32_t(argument),
		pointer(path), C.int32_t(len(path)), pointer(s.buffer), C.int32_t(len(s.buffer))))
	if n < 0 || n > len(s.buffer) {
		s.err = fmt.Errorf("invalid callback result %d", n)
		return -1
	}
	return n
}

func dominates(left, right []C.int32_t) bool {
	for i, value := range right {
		if left[i] < value {
			return false
		}
	}
	return true
}

// visitNode reports whether the node yields a frame and whether a dominating visit already covers it.
func (s *actionSearch) visitNode(path []C.int32_t, scores ...C.int32_t) (yield, pruned bool) {
	n := s.query(path, visit, 0)
	if n < 2 {
		if s.err == nil {
			s.err = fmt.Errorf("missing visit key")
		}
		return false, true
	}
	yield = s.buffer[0] != 0
	key := s.buffer[1]
	budget := append(s.buffer[2:n], scores...)
	s.frontier.Lock()
	defer s.frontier.Unlock()
	frontier, exists := s.frontier.visited[key]
	if len(budget) == 0 {
		s.frontier.visited[key] = nil
		return yield, exists
	}
	for _, previous := range frontier {
		if len(previous) != len(budget) {
			s.err = fmt.Errorf("visit budget dimensions changed")
			return false, true
		}
		if dominates(previous, budget) {
			return false, true
		}
	}
	for i := len(frontier) - 1; i >= 0; i-- {
		if dominates(budget, frontier[i]) {
			frontier = append(frontier[:i], frontier[i+1:]...)
		}
	}
	s.frontier.visited[key] = append(frontier, append([]C.int32_t(nil), budget...))
	return yield, false
}

func snapshot(path []C.int32_t, phase C.int32_t) searchFrame {
	return searchFrame{path: append([]C.int32_t(nil), path...), phase: phase}
}

func (s *actionSearch) dfs(path []C.int32_t, phase C.int32_t, deferBranches bool) {
	cursor := newCursor(s, path, phase, deferBranches)
	decision := 0
	for {
		frame, ok := cursor.next(decision)
		if !ok {
			return
		}
		s.frames = append(s.frames, frame)
		decision = s.query(frame.path, decide, int(frame.phase))
	}
}

func (s *actionSearch) newWorker(id int) *actionSearch {
	return &actionSearch{
		worker: id, rules: s.rules, families: s.families,
		buffer: make([]C.int32_t, len(s.buffer)), frontier: s.frontier,
	}
}

func (s *actionSearch) parallel(tasks []searchFrame, workers int, deferBranches bool) {
	if len(tasks) == 0 || s.err != nil || s.stopped {
		return
	}
	workers = min(workers, len(tasks))
	queue := make(chan searchFrame, len(tasks)-workers)
	for _, task := range tasks[workers:] {
		queue <- task
	}
	close(queue)
	results := make([]*actionSearch, workers)
	var done sync.WaitGroup
	for index := 0; index < workers; index++ {
		worker := s.newWorker(index + 1)
		results[index] = worker
		seed := tasks[index]
		done.Add(1)
		go func() {
			defer done.Done()
			run := func(task searchFrame) {
				path := make([]C.int32_t, len(task.path), hardAbortDepth)
				copy(path, task.path)
				worker.dfs(path, task.phase, deferBranches)
			}
			run(seed)
			for task := range queue {
				if worker.err != nil || worker.stopped {
					break
				}
				run(task)
			}
		}()
	}
	done.Wait()
	for _, worker := range results {
		s.frames = append(s.frames, worker.frames...)
		s.deferred = append(s.deferred, worker.deferred...)
		if worker.err != nil {
			s.err = worker.err
		}
	}
}

//export bridge_search
func bridge_search(
	familyCount, startPriority, capacity, workers C.int32_t, rules C.bridge_search_rules,
	handle *C.uint64_t,
) C.int32_t {
	if familyCount <= 0 || capacity < 2 || workers < 0 || workers > 64 || rules == nil || handle == nil {
		return failed
	}
	s := actionSearch{
		rules:    rules,
		families: int(familyCount),
		buffer:   make([]C.int32_t, int(capacity)),
		frontier: &visitFrontier{visited: make(map[C.int32_t][][]C.int32_t)},
		split:    workers > 0,
	}
	phase := C.int32_t(phaseRemaining)
	if startPriority != 0 {
		phase = phasePriority
	}
	s.dfs(make([]C.int32_t, 0, hardAbortDepth), phase, startPriority != 0)
	if workers > 0 {
		s.parallel(s.tasks, int(workers), startPriority != 0)
		remaining := s.deferred
		s.deferred = nil
		s.parallel(remaining, int(workers), false)
	} else {
		for index := 0; index < len(s.deferred) && !s.stopped && s.err == nil; index++ {
			branch := s.deferred[index]
			path := make([]C.int32_t, len(branch.path), hardAbortDepth)
			copy(path, branch.path)
			s.dfs(path, phaseRemaining, false)
		}
	}
	if s.err != nil {
		return failed
	}
	sessions.Lock()
	defer sessions.Unlock()
	sessions.next++
	sessions.all[sessions.next] = &session{frames: s.frames}
	*handle = C.uint64_t(sessions.next)
	return accepted
}

//export bridge_search_count
func bridge_search_count(handle C.uint64_t) C.int32_t {
	s := find(handle)
	if s == nil {
		return failed
	}
	return C.int32_t(len(s.frames))
}

// Each page entry is [phase, depth, path...].
//
//export bridge_search_page
func bridge_search_page(
	handle C.uint64_t, start, limit C.int32_t,
	output *C.int32_t, capacity C.int32_t, used *C.int32_t,
) C.int32_t {
	s := find(handle)
	if s == nil || start < 0 || int(start) > len(s.frames) ||
		limit <= 0 || output == nil || capacity <= 0 || used == nil {
		return failed
	}
	buffer := unsafe.Slice(output, int(capacity))
	index, count := 0, C.int32_t(0)
	for offset := int(start); offset < len(s.frames) && count < limit; offset++ {
		frame := s.frames[offset]
		size := 2 + len(frame.path)
		if len(buffer)-index < size {
			if count == 0 {
				return failed
			}
			break
		}
		buffer[index] = frame.phase
		buffer[index+1] = C.int32_t(len(frame.path))
		index += 2
		index += copy(buffer[index:], frame.path)
		count++
	}
	*used = C.int32_t(index)
	return count
}

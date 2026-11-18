package main

/*
#include "bridge.h"
*/
import "C"

import (
	"fmt"
	"unsafe"
)

type cursorNode struct {
	searchFrame
	entered    bool
	family     int
	candidates []C.int32_t
	candidate  int
}

// The stack pauses immediately after a yield; no callbacks run between next calls.
type searchCursor struct {
	search        *actionSearch
	stack         []cursorNode
	deferBranches bool
	awaiting      bool
}

func newCursor(s *actionSearch, path []C.int32_t, phase C.int32_t, deferBranches bool) *searchCursor {
	return &searchCursor{
		search: s, deferBranches: deferBranches,
		stack: []cursorNode{{searchFrame: snapshot(path, phase)}},
	}
}

func (c *searchCursor) next(decision int) (searchFrame, bool) {
	s := c.search
	if c.awaiting {
		c.awaiting = false
		switch decision {
		case 0:
		case 1:
			c.stack = c.stack[:len(c.stack)-1]
		case 2:
			s.stopped = true
		default:
			s.err = fmt.Errorf("invalid frame decision")
		}
	}
	for len(c.stack) > 0 && !s.stopped && s.err == nil {
		node := &c.stack[len(c.stack)-1]
		if !node.entered {
			node.entered = true
			if len(node.path) > maxSearchDepth || len(node.path) >= hardAbortDepth {
				c.stack = c.stack[:len(c.stack)-1]
				continue
			}
			yield, pruned := s.visitNode(node.path)
			if pruned {
				c.stack = c.stack[:len(c.stack)-1]
				continue
			}
			if yield {
				c.awaiting = true
				return node.searchFrame, true
			}
		}
		if node.candidates == nil {
			if node.family == s.families {
				c.stack = c.stack[:len(c.stack)-1]
				continue
			}
			count := s.query(node.path, discover, node.family)
			if count < 0 {
				break
			}
			node.family++
			if count == 0 {
				continue
			}
			node.candidates = append([]C.int32_t(nil), s.buffer[:count]...)
			node.candidate = 0
		}
		if node.candidate == len(node.candidates) {
			node.candidates = nil
			continue
		}
		candidate := node.candidates[node.candidate]
		node.candidate++
		legal := s.query(node.path, apply, int(candidate))
		if legal < 0 {
			break
		}
		if legal == 0 {
			continue
		}
		path := append(append([]C.int32_t(nil), node.path...), candidate)
		if c.deferBranches {
			isPriority := s.query(path, priority, 0)
			if isPriority < 0 {
				break
			}
			if isPriority == 0 {
				s.deferred = append(s.deferred, snapshot(path, phaseRemaining))
				continue
			}
		}
		if s.split {
			s.tasks = append(s.tasks, snapshot(path, node.phase))
		} else {
			c.stack = append(c.stack, cursorNode{searchFrame: searchFrame{path: path, phase: node.phase}})
		}
	}
	return searchFrame{}, false
}

//export bridge_search_start
func bridge_search_start(families, priority, capacity C.int32_t, handle *C.uint64_t) C.int32_t {
	if families <= 0 || capacity < 2 || handle == nil {
		return failed
	}
	s := &actionSearch{
		families: int(families), buffer: make([]C.int32_t, int(capacity)),
		frontier: &visitFrontier{visited: make(map[C.int32_t][][]C.int32_t)},
	}
	phase := C.int32_t(phaseRemaining)
	if priority != 0 {
		phase = phasePriority
	}
	sessions.Lock()
	defer sessions.Unlock()
	sessions.next++
	sessions.all[sessions.next] = &session{cursor: newCursor(s, nil, phase, priority != 0)}
	*handle = C.uint64_t(sessions.next)
	return accepted
}

//export bridge_search_next
func bridge_search_next(
	handle C.uint64_t, rules C.bridge_search_rules, decision C.int32_t,
	output *C.int32_t, capacity C.int32_t, used *C.int32_t,
) C.int32_t {
	state := find(handle)
	if state == nil || state.cursor == nil || rules == nil || decision < 0 || decision > 2 ||
		output == nil || capacity < hardAbortDepth+2 || used == nil {
		return failed
	}
	s := state.cursor.search
	s.rules = rules
	defer func() { s.rules = nil }()
	frame, ok := state.cursor.next(int(decision))
	for !ok && !s.stopped && s.err == nil && len(s.deferred) > 0 {
		branch := s.deferred[0]
		s.deferred[0] = searchFrame{}
		s.deferred = s.deferred[1:]
		state.cursor = newCursor(s, branch.path, phaseRemaining, false)
		frame, ok = state.cursor.next(0)
	}
	if s.err != nil {
		return failed
	}
	*used = 0
	if !ok {
		return 0
	}
	buffer := unsafe.Slice(output, int(capacity))
	buffer[0], buffer[1] = frame.phase, C.int32_t(len(frame.path))
	*used = C.int32_t(2 + copy(buffer[2:], frame.path))
	return accepted
}

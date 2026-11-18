package main

/*
#include "bridge.h"
*/
import "C"

import (
	"runtime"
	"runtime/debug"
	"sync"
)

//export bridge_allocated
func bridge_allocated() C.uint64_t {
	var stats runtime.MemStats
	runtime.ReadMemStats(&stats)
	return C.uint64_t(stats.TotalAlloc)
}

//export bridge_collect
func bridge_collect() {
	runtime.GC()
}

//export bridge_runtime_info
func bridge_runtime_info(parallelism, race *C.int32_t) C.int32_t {
	if parallelism == nil || race == nil {
		return failed
	}
	info, ok := debug.ReadBuildInfo()
	if !ok {
		return failed
	}
	*parallelism = C.int32_t(runtime.GOMAXPROCS(0))
	*race = 0
	for _, setting := range info.Settings {
		if setting.Key == "-race" && setting.Value == "true" {
			*race = 1
		}
	}
	return accepted
}

const (
	failed   = -1
	accepted = 1
)

type session struct {
	frames []searchFrame
	cursor *searchCursor
}

var sessions = struct {
	sync.Mutex
	next uint64
	all  map[uint64]*session
}{all: make(map[uint64]*session)}

// No lock is held across a managed callback. A handle is used synchronously by
// one caller; the C# owner must not dispose or advance it concurrently.
func find(handle C.uint64_t) *session {
	sessions.Lock()
	defer sessions.Unlock()
	return sessions.all[uint64(handle)]
}

//export bridge_destroy
func bridge_destroy(handle C.uint64_t) C.int32_t {
	sessions.Lock()
	defer sessions.Unlock()
	if _, ok := sessions.all[uint64(handle)]; !ok {
		return failed
	}
	delete(sessions.all, uint64(handle))
	return accepted
}

func main() {}

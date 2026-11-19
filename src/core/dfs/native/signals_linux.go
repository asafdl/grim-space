package main

/*
#include <errno.h>
#include <signal.h>
#include <stddef.h>

static int bridge_disable_clr_signal_altstack(void) {
	const int signals[] = { SIGRTMIN, SIGTRAP };
	for (unsigned int i = 0; i < sizeof(signals) / sizeof(signals[0]); i++) {
		struct sigaction action;
		if (sigaction(signals[i], NULL, &action) != 0)
			return errno;
		if ((action.sa_flags & SA_ONSTACK) == 0)
			continue;
		action.sa_flags &= ~SA_ONSTACK;
		if (sigaction(signals[i], &action, NULL) != 0)
			return errno;
	}
	return 0;
}
*/
import "C"

import (
	"fmt"
	"os"
	"syscall"
)

func init() {
	switch value := os.Getenv("GRIM_SPACE_GO_SIGNAL_WORKAROUND"); value {
	case "", "0":
		return
	case "1":
	default:
		panic("GRIM_SPACE_GO_SIGNAL_WORKAROUND must be 0 or 1")
	}

	// Go has installed its signal flags before package initialization.
	if code := C.bridge_disable_clr_signal_altstack(); code != 0 {
		panic(fmt.Sprintf("Go search Linux signal workaround failed: %v", syscall.Errno(code)))
	}
	fmt.Fprintln(os.Stderr, "Go search: experimental Linux signal workaround enabled (SIGRTMIN, SIGTRAP)")
}

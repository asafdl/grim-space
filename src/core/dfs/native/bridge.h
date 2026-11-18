#ifndef GRIM_SPACE_SEARCH_H
#define GRIM_SPACE_SEARCH_H

#include <stdint.h>

typedef int32_t (*bridge_search_rules)(
	int32_t worker, int32_t operation, int32_t argument,
	const int32_t *path, int32_t depth, int32_t *output, int32_t capacity);

static inline int32_t bridge_query(
	bridge_search_rules rules, int32_t worker, int32_t operation, int32_t argument,
	const int32_t *path, int32_t depth, int32_t *output, int32_t capacity) {
	return rules(worker, operation, argument, path, depth, output, capacity);
}

#endif

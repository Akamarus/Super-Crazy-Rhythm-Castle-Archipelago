"""Emit current production slot data for the client contract integration tests."""
import json
from test_world_integration import WorldIntegrationTests

fixture = WorldIntegrationTests()
fixture.setUp()
try:
    print(json.dumps([fixture.build_world(difficulty=value).fill_slot_data() for value in range(4)]))
finally:
    fixture.doCleanups()

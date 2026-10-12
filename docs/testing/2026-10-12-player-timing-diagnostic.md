# Player movement timing diagnostic candidate

The October 12 Music Lab and Meat Dimension captures show 18 and 9 samples respectively with full accepted input, control enabled, and near-zero desired movement. Each has raw intended movement below 0.01 units. The native motor resets its acceleration timer below a small-motion threshold. These samples establish a repeatable symptom, not the originating cause.

F8 now additionally observes PlayerCharacter.RunUpdateWithDeltaTime(float) at the primitive native boundary. It records actor pointer, rendered frame, and exact supplied elapsed time, preserving zero/invalid values. This is explicitly player-update timing, not a direct motor hook; the inspected native path forwards that argument into input movement. Existing 10 Hz samples now format Vector2 moveSpeed and moveSpeedRatio rather than reporting them unavailable.

The native original executes exactly once with unchanged arguments. The observer makes no gameplay, physics, graphics, or save writes. Recording uses a fixed 16,384-entry buffer; overflow is counted, not allocated. Stop, room exit, capture failure and component destruction deactivate observation. A lazily installed pass-through hook remains until plugin unload, avoiding repeated detour churn. Callback instrumentation errors disable recording without suppressing native movement. Detailed formatting and file writing occur after capture.

Installed with approval and verified through normal and faulty native captures; later retained in the installed movement candidate. Recorder regression tests cover inactivity, limits, overflow, same-frame calls, invalid/zero timing, stop and reset. No test seed or save reset was required.

Next: install after approval with game closed, reproduce movement in either hub with F8 for about 12 seconds, then inspect per-frame supplied timing alongside movement intent and speed settings. Do not cap FPS or alter acceleration based only on the current evidence.

# October 11 client readiness repair — candidate only

The reported Plunger/Meoo delay was traced to `WithStableQuestItemSave` inheriting a cassette gameplay gate that opened only after observing a live Music Lab phone bank. A loaded Lobby or other area could therefore remain gated despite a stable selected save.

The keeper now observes the exact spawned-player scene path while `GameRoomEnquiries.InARunningRoom()` is true. Discovery/observation is limited to four times per second, cached scene objects are reused, and the existing gate still requires fresh marker incarnation tied to the exact generation, epoch, slot and save pointer. Selected-save synchronization and pointer readback are unchanged; missing evidence keeps the gate closed. There are no new native hooks or save writes in marker discovery.

A second fault path consumed a one-shot reconciliation request while native synchronization/status was temporarily unavailable. Without another event it could remain pending indefinitely. A two-second managed retry now revisits pending work after gameplay readiness, and still honors the existing native grant-attempt cap, delayed verification and deposited-item protections. Pending-song enumeration occurs only when the retry interval elapses, not each frame. This is a repair candidate for restart-dependent receipt reports, not proof that every Zen occurrence shared this cause.

Tests reproduced the Music Lab-only discovery failure and missing bounded retry before repair. The 31 runtime/performance checks and full cassette save-boundary suite pass. The client Release build passes; game acceptance remains pending.

Acceptance: load a test save directly outside Music Lab, receive an item, perform Plunger pickup/hand-in or Meoo hand-in before visiting Music Lab, and verify the AP check promptly. Receive and deposit a new cassette without restart, then revisit/reconnect and verify no duplicate reward. Save switch/reload must not read or write the prior save. The F5 Music Lab-specific diagnostic retains its original phone-bank guard.
## October 11 combined verification

Final combined candidate: 40 client regression projects passed after the independent review repair. The retry-overlap regression failed before ordinary observations were prevented from bypassing delayed verification; it now passes and retains the persistence wave. Release build passes with zero errors and NU1900 vulnerability-feed lookup warning. Repository validator passes. Tracker work passed 177 Python tests, actual AP0.6.8/UT0.2.32 1,888-state comparison, 37-case matrix and nine offline packet cases. Native acceptance is pending.

Local package: outputs/release-v0.29.0-candidate-20261011 in the task workspace. APWorld SHA256 32E65EC6262155B63C1E9B83C36F7D426C13C03687063A9803371D90BBC1335A; client SHA256 BDC16309FED16BD12F18AEF0051ADF4BD5649A697AEB2368C3AD44AD9096A061. Installed client hash remains 64679038FBCE209E35D54DDFCDECFB11C0241BE13A53E01CF72DB2EA670CE917. Nothing installed, published, or committed.

## Roots phone-booth visual acceptance — October 11

The user confirmed they had already checked the Roots phone-booth presentation and signed off that the visual obstruction was fixed. This is user-reported gameplay acceptance from the prior observation, not a new capture or a replay of the already-collected source. No save reset was performed.

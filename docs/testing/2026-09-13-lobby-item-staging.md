# Lobby item and action-check staging (2026-09-13)

No gameplay or save changes were made for this stage. The three AP item names and
permanent IDs are registered, but none are generated in the item pool. The two
pickup source IDs are reserved but do not appear in the active region graph.
`randomize_lobby_letters_bean_trumpet` and
`randomize_demolition_certificate` are both `false`.

## Evidence and intended behavior

- The Important Letters pickup in `GameRoom_Hub1A` grants the task item and
  `BEAN_TRUMPET_ABILITY` together, and sets
  `LOBBY_HUB_MENIAL_TASK_ITEM_COLLECTED`. The pure client policy stages two
  distinct source names and guards against re-granting delivered Letters after
  `LOBBY_HUB_MENIAL_TASK_ITEM_DEPOSITED` is set. It is not wired into runtime.
- The Demolition Certificate is granted just before the Level 7 result through
  `LOBBY_HUB_TOOLS_CERTIFICATE`. This is also the only observed persistent
  marker. Suppressing it would remove the only currently known recovery source;
  the captured save probe did not establish a separate persisted level-result
  source. Leave this item vanilla until that path is proven.
- A native collected marker alone does not establish which AP seed/save owns a
  pickup. Before enabling the Letters/Bean pair, bind its check replay to the
  active seed and save slot, including a restart while disconnected. Otherwise
  an old save could send a check into a new seed, or an offline pickup could be
  lost when the process exits.
- Lobby plunger use (`LOBBY_HUB_MEAT_DOOR_BLOCKER_PLUNGERED`) and Lobby Star
  Eater feed (`LOBBY_HUB_STAR_EATER_FED`) are separate read-only diagnostic
  candidates. The Star Eater checks a threshold without consuming Stars; the
  threshold is not yet verified. Neither action has an AP location ID or sends
  an AP check.

## Activation gate

Implement seed/save-bound source recovery and certificate result persistence,
then test an offline pickup, restart, reconnect, and new-seed/new-save isolation.
Only then add runtime suppression/receipt wiring, active AP locations and pool
entries, and a manual acceptance pass. Do not advertise these items or action
checks as randomized in a testing release before that gate passes.

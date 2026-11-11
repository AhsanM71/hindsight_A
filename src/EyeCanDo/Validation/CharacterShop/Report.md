# EyeCanDo — Character & Shop implementation

Implemented and verified in Unity **6000.6.8f1**. The Editor is left on the saved Start scene in Edit mode. The final compilation has no errors. Six pre-existing obsolete-API warnings remain in the legacy `NavigationSmokeTest.cs` (`FindObjectsSortMode` overloads); the new feature introduces no compiler warnings. The exact original username and all customization keys (including absence) were restored after testing.

## Delivered

- Original articulated storybook child replaces the Start owl and appears as a small idle avatar on the map lawn at `(-4.4, 0.1, 3.4)`. Existing scene cameras and clinic scenery remain unchanged. The map avatar has no colliders.
- Four face choices, six skin tones, five hairstyles, six hair colours, and six outfit palettes. Defaults are medium-brown skin, dark curls, teal clothing, and no accessories.
- Separate head, face, hair, hat, glasses, hand, and back attachments; compatible crown/side hair sections; per-instance material property blocks; idle, blinking, entry wave, and purchase celebration.
- Dedicated Shop scene with five pictorial categories, six cards per page, explicit pagination, colour swatches, 45-degree rotation, and rear three-quarter backpack preview. Thumbnails are authored PNGs; there is one runtime scene camera.
- One temporary item preview; category/item replacement and Back/Escape cancellation. Buy saves ownership and equips. Free and owned items equip without spending. Equipped checks and disabled insufficient-funds actions are visible.
- Catalogue prices: cap 40, glasses 50, star hat 60, backpack 80. Free removal for each accessory slot. No reward gameplay is connected.
- Pure C# customization service with injected storage/codec, one-time 200-coin initialization for new and username-only profiles, candidate-before-event persistence, complete versioned JSON, last-good backup, grant marker, bounded positive coin addition, and retry after failed saves. Unknown owned IDs survive load; invalid equipment uses free defaults; unrecoverable saves receive zero coins.
- Existing `JourneyMapController.OpenShop()` now routes through `SceneNavigator.GoToShop()`. Four build scenes are registered. Adventure authoring and validation handle the replaced owl and removed placeholder.

## Executed verification

| Evidence | Result |
| --- | --- |
| [Service checks](service-checks.json) | 31 passed: initialization/relaunch, purchases, free equipment, duplicate calls, shortfalls, exact balance, failure/retry, malformed data, backup recovery, missing IDs, version rejection and overflow |
| [Interaction regression](Interactions/interactions.json) | 1,205 passed through Input System and EventSystem; simulated mouse and touch; advancing frames 2–38,043 |
| [Native mouse](native-mouse.json) | Start → map → Shop → cap preview → Buy → map; 200 → 160 coins and the equipped cap visible on map |
| [Assets and idempotence](assets-and-idempotence.json) | 69 passed; missing scripts/materials/shaders, catalogue and attachments, four scenes, original Shop callback, repeated Apply |
| [Attachment matrix](compatibility.json) | 23 passed/captured: every 5 × 3 hair/hat pairing with glasses, four faces, backpack views at 0/45/135/180 degrees |
| [Catalogue extension](catalogue-extension.json) | Additional cosmetic ID survives authoring; temporary fixture removed |
| [Actual process restart](restart.json) | Purchase and 160-coin balance survived process 55231 → 57010 |
| [Final state](final-state.json) | Compile passed; Start clean; no pending recovery file; exact original profile restored |
| [Final console](final-console-status.json) | 0 errors; 6 pre-existing legacy API deprecation warnings |

The interaction regression also checked setup, blank-name rejection, accented and long names, Settings save/cancel, greeting updates, returning-player routing, all three journeys, and all nine Coming Soon actions. Screenshots cover 1920×1080 and 1280×720, plus three simulated safe-area shapes. Active Shop targets remained at least 80 pixels in the safe-area checks. Small controls use 144 reference units to retain that minimum with simulated insets.

[Map before](map-before.json) and [map after](map-after.json) match exactly after excluding the approved Map Avatar root and existing screen UI. World objects, destinations, camera, lighting, materials, and colliders are included in that comparison. Separate callback and idempotence checks cover the UI integration.

## Visual evidence

- [Start, 1920×1080](Interactions/start-1920x1080.png)
- [Map, 1280×720](Interactions/map-1280x720.png)
- [Shop free choices, 1920×1080](Interactions/shop-free-1920x1080.png)
- [Purchasable item, 1280×720](Interactions/shop-purchasable-1280x720.png)
- [Equipped item, 1920×1080](Interactions/shop-equipped-1920x1080.png)
- [Insufficient funds, 1280×720](Interactions/shop-insufficient-1280x720.png)
- [Hair/hat contact sheet](Compatibility/hat-hair-matrix.png): rows bob, curls, no hair, puffs, short; columns cap, no hat, star hat.

The remaining requested-resolution, safe-area, Settings, and journey images are in `Interactions/`. Individual attachment renders are in `Compatibility/`. Native mouse output is also saved in `native-mouse-map.png`.

## Reapply and extend

Use **EyeCanDo → Character & Shop → Apply** from a clean scene in Edit mode. It authors the feature and updates Start/Map without running the MVP or garden rebuilds. **Run service checks** runs the injected-store checks without touching the real profile. The Adventure validation menu now uses the full profile-protection wrapper.

Assets and code are under `Assets/EyeCanDo/AvatarShop`. Add cosmetic definitions and prefabs to `Catalog/Cosmetics.asset` for runtime expansion. Apply refreshes the built-in starter definitions and preserves additional catalogue IDs. Accessory payloads use the existing attachment slots, so they do not require character-controller changes. Regenerate authored thumbnails when payloads change.

## Sources and limits

All delivered character and cosmetic meshes are original procedural Unity artwork. No Quaternius models or animations were imported. The downloaded Standard archive manifests, inspection listings, READMEs, and bundled CC0 licenses are preserved in [Research](Research/download-manifest.json). The free packages did not supply the chosen child proportions, everyday outfits, or wave, so the authorized original-mesh approach was used. Existing project URP, Fredoka, uGUI, safe-area, Input System, and fade assets were reused.

Physical-device rendering/performance remains unverified. Storage failures were tested with injected exceptions; no physical disk-full failure was induced. No paid assets, real-money purchases, ads, loot boxes, reward minigames, scoreboard, or capstone edits were added.


# EyeCanDo PoC — Usability Feedback and Suggested Improvements

## 1. Progress Bar Visibility and Placement

**Observation:** The progress bar may be distracting to children and could give the impression that it is an interactive element they should press.

**Suggested Improvements:**

- Consider removing the progress bar from the child-facing gameplay interface.
- Alternatively, make progress information accessible only to parents or caregivers.
- Ensure the progress bar does not obstruct important gameplay elements.

## 2. Green Menu Button

**Observation:** Children may associate the colour green with "Go" or "Start." A green Menu button could therefore create confusion about its purpose.

**Suggested Improvements:**

- Consider using a different colour for the Menu button.
- Include a recognizable menu or home icon to communicate its function more clearly.
- Reserve green for actions that indicate starting or continuing an activity.

## 3. Interacting with Sam

**Observation:** When Sam appears and prompts the child to click on them, it may not be immediately obvious that Sam is interactive.

Although the arrow above Sam provides a visual cue, the progress bar may obstruct or compete with this guidance.

**Suggested Improvements:**

- Make Sam's interactive state more visually apparent.
- Remove or reposition the progress bar to reduce distractions.
- Consider using a gentle animation or visual highlight to draw attention to Sam.

## 4. Eye-Drop Interaction — Simulating Keeping Eyes Open

**Observation:** The eye-drop interaction could be improved to provide a more realistic and interactive simulation of keeping one's eyes open during the procedure.

**Proposed Interaction:**

1. **Holding the button:** When the child presses and holds the designated button, the eyes remain open and the progress timer moves forward.
2. **Releasing the button:** If the child releases the button before completing the activity, the progress timer gradually moves backwards instead of immediately resetting.
3. **Gradual eye closure:** As the timer decreases, the eyes begin to close through an eyelid animation. The speed of the closure depends on the remaining progress, allowing the eyes to close quickly or slowly.
4. **Fully closed eyes:** Once the timer returns to its initial value, the eyes close completely and the screen becomes black.
5. **Reopening the eyes:** As soon as the child presses the button again, the eyes immediately open, the black screen disappears, and the progress timer begins increasing from its current value.

**Intended Benefits:**
- Creates a more interactive simulation of opening and closing one's eyes.
- Provides visual feedback that corresponds directly to the child's actions and progress.
- Encourages the child to maintain the button hold without introducing harsh penalties or an immediate reset.
- Makes the interaction more engaging and intuitive through gradual eyelid animations.

**Consideration:** The activity should remain forgiving and non-punitive, with no failure state if the child releases the button. The eye-closing animation should also be gentle and visually comfortable for young children.

## 5. End-of-Game Navigation Buttons

**Observation:** The green buttons displayed at the end of a preparation game may make it difficult for children to distinguish between available actions.

**Suggested Improvements:**

- Use distinct colours and recognizable icons for different actions.
- Clearly distinguish between replaying the activity, returning to the Journey Map, and continuing.
- Make the primary recommended action visually prominent.

## 6. Visual Guidance When a Child Is Stuck

**Observation:** Children may occasionally be unsure which button to press or which object to interact with.

**Suggested Improvement:**

Introduce a gentle pulsating animation on the expected interactive element when the child has been inactive or appears stuck.

Potential behaviour:

- After a short period without interaction, the relevant button or object begins to pulse.
- The animation draws attention without being overly distracting.
- The pulsating effect stops once the child interacts with the element.

**Intended Benefit:** Provide intuitive visual guidance while reducing the need for repeated verbal instructions.

---

## Overall Feedback Summary

The main opportunities for improvement involve **reducing visual distractions, making interactive elements easier to recognize, and providing clearer guidance for young children**.

The proposed changes aim to create a more intuitive and child-friendly experience while keeping interactions accessible, supportive, and non-punitive.


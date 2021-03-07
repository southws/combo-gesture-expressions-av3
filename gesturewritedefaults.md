# Write Defaults

The VRChat Documentation recommends using Write Defaults OFF.

ComboGestureExpressions will generate all nodes with Write Defaults OFF.

However, when using the *Gesture Playable Layer* to animate transforms such as cat ears, tail or wings, extra care needs to be taken in order for these animations to work properly.

## Avatar mask

In particular, all layers of the FX controller need to have a special mask that will prevent transform animation.

ComboGestureExpressions will generate an Avatar Mask that will be used for all layers of the FX Animator controller that don't have a mask yet.

## Transforms capture

With Write Defaults OFF, the animations need to know how to reset the transforms when they are no longer animated.

ComboGestureExpressions will generate animations by looking at the current transforms of the avatar.

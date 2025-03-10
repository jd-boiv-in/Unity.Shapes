# Unity.Shapes

Draw debug shapes for debugging in Unity. Not meant for non-debugging purpose, altho it should be pretty fast and looks pretty smooth. It can be included in build unlike gizmos. Will be drawn over the background queue but before any transparent sprites, also non-alloc.

## Installation

Add these dependencies to your `manifest.json`

```json
{
  "dependencies": {
    "jd.boiv.in.shapes": "https://github.com/starburst997/Unity.Shapes.git",
    "jd.boiv.in.extensions": "https://github.com/starburst997/Unity.Extensions.git",
    "jd.boiv.in.colors": "https://github.com/starburst997/Unity.Colors.git"
  }
}
```

## Shapes

- Circle
- Quad
- Polygons
- Line
- Label

## Usage

Call your code from anywhere (every frame).

```csharp
Shape.Circle(center, 0.5f, Color.red);
```

To have the shapes appears in the editor while the game isn't running, you can do like this:

```csharp
[ExecuteInEditMode]
public class TestShapes : MonoBehaviour
{
    public float Radius = 10f;
    
    public void Update()
    {
        Shape.Circle(center, Radius, Color.green);
    }
}
```

## Samples

A [sample project](https://github.com/starburst997/Unity.Shapes/tree/main/Samples~/Shapes%20Sample) is also available.

## TODO

- Add more examples
- Better readme

## Credits

Forked from [miguel12345/UnityShapes](https://github.com/miguel12345/UnityShapes) && [nukadelic/UnityQuickDraw](https://github.com/nukadelic/UnityQuickDraw)
# Unity.Shapes

Draw debug shapes for debugging in Unity. Not meant for non-debugging purpose, altho it should be pretty fast and looks pretty smooth. It can be included in build unlike gizmos.

## Installation

Add the dependency to your `manifest.json`

```json
{
  "dependencies": {
    "jd.boiv.in.shapes": "https://github.com/starburst997/Unity.Shapes.git"
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
Circle.Draw(new CircleInfo{
    Center = new Vector3(UserPosition.position.x, UserPosition.position.y, 0),
    Forward = new Vector3(0, 0, 1),
    Radius = 10f,
    FillColor = new Color(1, 0, 0, 0.2f),
    BorderColor = new Color(0, 0, 0, 1),
    BorderWidth = 0.1f,
    Bordered = true,
});
```

To have the shapes appears in the editor while the game isn't running, you can do like this:

```csharp
[ExecuteInEditMode]
public class TestShapes : MonoBehaviour
{
    public float Radius = 10f;
    
    public void Update()
    {
        Circle.Draw(new CircleInfo{
            Center = transform.position,
            Forward = new Vector3(0, 0, 1),
            Radius = Radius,
            FillColor = new Color(0, 1, 0, 0.33f),
            Bordered = false,
        });
    }
}
```

## Samples

A [sample project](https://github.com/starburst997/Unity.Shapes/tree/main/Samples~/Shapes%20Sample) is also available.

## TODO

- Add more examples
- Better readme
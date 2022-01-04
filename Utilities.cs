using System;
using Godot;

namespace Kesa.Conveyors;

public static class Utilities
{
    public static Vector2 Align(this Vector2 source, float offset = 64)
    {
        var halfOffset = offset / 2;
        var xOffs = source.x % offset;
        var yOffs = source.y % offset;
        return new Vector2(source.x - xOffs + halfOffset, source.y - yOffs + halfOffset);
    }
    
    public static (Vector2 Point, Vector2 Difference,  Vector2 Direction) Project(this Line2D line, Vector2 point)
    {
        if (line.Points.Length != 2)
        {
            throw new InvalidOperationException("can only project a point onto a line with two points.");
        }
        
        var p1 =line.GlobalTransform * line.Points[0];
        var p2 =line.GlobalTransform * line.Points[1];
        var pResult=  Geometry.GetClosestPointToSegment2d(point, p1, p2);
        var pDiff = pResult - point;
        return (pResult, pDiff, pDiff.Normalized());
    }
}
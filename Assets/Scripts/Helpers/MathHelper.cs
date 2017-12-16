using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MathHelper {
	
	// a1 is line1 start, a2 is line1 end, b1 is line2 start, b2 is line2 end
	public static bool LineSegmentsIntersect(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, out Vector2 intersection)
	{
		intersection = Vector2.zero;

		Vector2 b = a2 - a1;
		Vector2 d = b2 - b1;
		float bDotDPerp = b.x * d.y - b.y * d.x;

		// if b dot d == 0, it means the lines are parallel so have infinite intersection points
		if (bDotDPerp == 0)
			return false;

		Vector2 c = b1 - a1;
		float t = (c.x * d.y - c.y * d.x) / bDotDPerp;
		if (t < 0 || t > 1)
			return false;

		float u = (c.x * b.y - c.y * b.x) / bDotDPerp;
		if (u < 0 || u > 1)
			return false;

		intersection = a1 + t * b;

		return true;
	}
}

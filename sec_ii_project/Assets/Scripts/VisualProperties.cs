using UnityEngine;


public static class VisualProperties
{
    // return the eccentricity (angular deviation from forward)
    public static float GetEccentricity(Transform viewTransform, GameObject g)
    {
        return Vector3.Angle(g.transform.position - viewTransform.position, viewTransform.forward);
    }

    // returns the depth (NOT distance) - this is the depth of the plane, parallel to the view, that the object lies on
    public static float GetDepth(Transform viewTransform, GameObject g)
    {
        // get the eccentricity
        float ecce = GetEccentricity(viewTransform, g);

        // get the distance between the object and the view
        float distance = Vector3.Distance(viewTransform.position, g.transform.position);

        // compute the depth as a double for precision, but return as a float
        return (float)(System.Math.Cos(ecce * (System.Math.PI / 180)) * distance);
    }

    private static void VisualAngleAssertions(GameObject g)
    {
        Debug.Assert(g.transform.localScale.x == g.transform.localScale.y); // uniformly scaled
        Debug.Assert(g.transform.localScale.x == g.transform.localScale.z);
        Debug.Assert((g.transform.parent == null) || (g.transform.localScale == g.transform.lossyScale)); // top level OR localScale == lossyScale (worldScale)
        Debug.Assert(g.GetComponent<SphereCollider>() != null); // must be a sphere collider on it
    }

    // currently ONLY guaranteed to work if g is top level in hierarchy - could be modified to take this into account
    // additionally, you MUST have a sphereCollider on the object that is sized such that it encompasses the object at scale 1, 1, 1
    public static float GetVisualAngle(Transform viewTransform, GameObject g)
    {
        // Must pass
        VisualAngleAssertions(g);

        // this equation is based on the fact that the visual angle of a sphere isn't simply the diameter --
        // see forum.unity.com/attachments/sphere-png.945259/
        double sphereColliderRadius = g.GetComponent<SphereCollider>().radius;
        double actualRadius = sphereColliderRadius * g.transform.localScale.x;
        double distance = (double)Vector3.Distance(viewTransform.position, g.transform.position);
        double visualAngleRadians = 2 * System.Math.Asin(actualRadius / distance);
        return (float)(visualAngleRadians * 180 / System.Math.PI);

    }


    // currently ONLY guaranteed to work if g is top level in hierarchy - could be modified to take this into account
    // additionally, you MUST have a sphereCollider on the object that is sized such that it encompasses the object at scale 1, 1, 1
    public static Vector3 GetScaleOfTopLevelObjectBasedOnDesiredVisualAngle(Transform viewTransform, GameObject g, float visualAngleDegrees)
    {
        // Must pass
        VisualAngleAssertions(g);

        float distance = Vector3.Distance(viewTransform.position, g.transform.position);

        // get the scaling factor - how much to scale the object if it was at its default size of 1, 1, 1
        // this equation is based on the fact that the visual angle of a sphere isn't simply the diameter --
        // see forum.unity.com/attachments/sphere-png.945259/
        double visualAngleRadians = visualAngleDegrees * System.Math.PI / 180;
        double halfAngle = visualAngleRadians / 2;

        // get the size of the radius we need for the sphere to be tangent
        double radiusSizeNeededForGazeTangency = System.Math.Sin(halfAngle) * distance; // gaze angle edge vector is tangent to sphere

        // get the current sphere radius
        double currentColliderRadius = g.GetComponent<SphereCollider>().radius;

        // compute a scaling factor
        double scalingFactor = radiusSizeNeededForGazeTangency / currentColliderRadius;

        return new Vector3((float)scalingFactor, (float)scalingFactor, (float)scalingFactor);
    }

    // currently ONLY guaranteed to work if g is top level in hierarchy - could be modified to take this into account
    // additionally, you MUST have a sphereCollider on the object that is sized such that it encompasses the object at scale 1, 1, 1
    public static void SetScaleForTopLevelObjectBasedOnDesiredVisualAngle(Transform viewTransform, GameObject g, float visualAngleDegrees)
    {
        // Must pass
        VisualAngleAssertions(g);

        float distance = Vector3.Distance(viewTransform.position, g.transform.position);

        // get the scaling factor - how much to scale the object if it was at its default size of 1, 1, 1
        // this equation is based on the fact that the visual angle of a sphere isn't simply the diameter --
        // see forum.unity.com/attachments/sphere-png.945259/
        double visualAngleRadians = visualAngleDegrees * System.Math.PI / 180;
        double halfAngle = visualAngleRadians / 2;

        // get the size of the radius we need for the sphere to be tangent
        double radiusSizeNeededForGazeTangency = System.Math.Sin(halfAngle) * distance; // gaze angle edge vector is tangent to sphere

        // get the current sphere radius
        double currentColliderRadius = g.GetComponent<SphereCollider>().radius;

        // compute a scaling factor
        double scalingFactor = radiusSizeNeededForGazeTangency / currentColliderRadius;

        // apply
        g.transform.localScale = new Vector3((float)scalingFactor, (float)scalingFactor, (float)scalingFactor);
    }

}

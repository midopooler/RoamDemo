using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;
using Unity.Mathematics;
using System.Linq;

public abstract class LevelAction
{
    public abstract void Undo(SplineContainer spline, List<GameObject> objects);
    public abstract void Redo(SplineContainer spline, List<GameObject> objects);
}

public class AddKnotAction : LevelAction
{
    private int index;
    private BezierKnot knot;

    public AddKnotAction(int index, BezierKnot knot)
    {
        this.index = index;
        this.knot = knot;
    }

    public override void Undo(SplineContainer spline, List<GameObject> objects)
    {
        spline.Spline.RemoveAt(index);
    }

    public override void Redo(SplineContainer spline, List<GameObject> objects)
    {
        spline.Spline.Insert(index, knot);
    }
}

public class MoveKnotAction : LevelAction
{
    private int index;
    private BezierKnot oldKnot;
    private BezierKnot newKnot;

    public MoveKnotAction(int index, BezierKnot newKnot)
    {
        this.index = index;
        this.oldKnot = newKnot;
        this.newKnot = newKnot;
    }

    public override void Undo(SplineContainer spline, List<GameObject> objects)
    {
        spline.Spline[index] = oldKnot;
    }

    public override void Redo(SplineContainer spline, List<GameObject> objects)
    {
        spline.Spline[index] = newKnot;
    }
}

public class PlaceObjectAction : LevelAction
{
    private GameObject obj;
    private Vector3 position;
    private Quaternion rotation;

    public PlaceObjectAction(GameObject obj, Vector3 position, Quaternion rotation)
    {
        this.obj = obj;
        this.position = position;
        this.rotation = rotation;
    }

    public override void Undo(SplineContainer spline, List<GameObject> objects)
    {
        objects.Remove(obj);
        Object.Destroy(obj);
    }

    public override void Redo(SplineContainer spline, List<GameObject> objects)
    {
        obj = Object.Instantiate(obj, position, rotation);
        objects.Add(obj);
    }
}

public class PlaceAllObjectsAction : LevelAction
{
    private List<GameObject> objects;
    private List<Vector3> positions;
    private List<Quaternion> rotations;

    public PlaceAllObjectsAction(List<GameObject> objects)
    {
        this.objects = new List<GameObject>(objects);
        this.positions = objects.Select(obj => obj.transform.position).ToList();
        this.rotations = objects.Select(obj => obj.transform.rotation).ToList();
    }

    public override void Undo(SplineContainer spline, List<GameObject> objects)
    {
        foreach (var obj in objects.ToList())
        {
            Object.Destroy(obj);
        }
        objects.Clear();
    }

    public override void Redo(SplineContainer spline, List<GameObject> objects)
    {
        for (int i = 0; i < this.objects.Count; i++)
        {
            GameObject newObj = Object.Instantiate(this.objects[i], positions[i], rotations[i]);
            objects.Add(newObj);
        }
    }
} 
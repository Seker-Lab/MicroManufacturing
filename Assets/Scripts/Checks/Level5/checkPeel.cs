using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class checkPeel : levelRequirementParent
{
    public override void onStart()
    {
        base.onStart();
        name = "Peel the mold.";
        description = "The etch toggle is in the right.";
        checkOutsideEdits = true;
    }

    public override void check()
    {
        if (!met)
        {
            if (GameObject.Find("Control").GetComponent<control>().peelCalled)
            {
                met = true;
            }
        }
    }
}

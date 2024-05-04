using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class checkCast : levelRequirementParent
{
    public override void onStart()
    {
        met = false;
        base.onStart();
        name = "Cast the mold.";
        description = "The cast toggle is in the right.";
        checkOutsideEdits = true;
    }

    public override void check()
    {
        GameObject np = GameObject.Find("New Process");
        if (np && !met)
        {
            if (np.GetComponent<ProcessCast>())
            {
                GameObject.Find("Control").GetComponent<control>().peelCalled = false;
                met = true;
            }
        }
    }
}

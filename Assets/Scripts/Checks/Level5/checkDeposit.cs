using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class checkDeposit : levelRequirementParent
{

    public override void onStart()
    {
        met = false;
        base.onStart();
        name = "Make a mold with the techniques you've learned.";
        description = "Make sure you have at least one deposit or photoresist.";
        checkOutsideEdits = true;
    }

    public override void check()
    {
        met = false;
        GameObject np = GameObject.Find("New Process");
        if (!np)
        {
            if (GameObject.Find("MeshGenerator(Clone)"))
            {
                met = true;
                GameObject.Find("Control").GetComponent<control>().peelCalled = false;
            }
        }
    }
}

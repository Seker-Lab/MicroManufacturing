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
        description = "Make sure you have at least one deposit.";
        checkOutsideEdits = true;
    }

    public override void check()
    {
        if (GameObject.Find("New Process"))
        {
            met = true;
        }
        else
        {
            met = met || false;
        }
    }
}

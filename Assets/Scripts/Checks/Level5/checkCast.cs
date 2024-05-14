using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions.Must;

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
        met = false;
        foreach (Transform child in GameObject.Find("LayerStack").transform)
        {
            meshMaterial meshMat = child.gameObject.GetComponent<meshMaterial>();
            if (meshMat&& meshMat.myMaterial == control.materialType.cast)
            {
                met = true;
            }
        }
            
    }
}

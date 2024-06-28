using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ColorTest : NetworkBehaviour
{
    private NetworkVariable<bool> m_netColorStatus = new(writePerm: NetworkVariableWritePermission.Owner);

    public SpriteRenderer spriteRenderer;
    public Color onColor;
    public Color offColor;
    public bool colorStatus;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    // Update is called once per frame
    void Update()
    {
        if (IsOwner)
        {

            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (colorStatus)
                {
                    colorStatus = false;
                    spriteRenderer.material.color = offColor;
                }
                else
                {
                    colorStatus = true;
                    spriteRenderer.material.color = onColor;
                }
            }
            m_netColorStatus.Value = colorStatus;
        }
        else
        {
            spriteRenderer.material.color = m_netColorStatus.Value ? onColor : offColor;
        }
    }

    void ChangeColor(bool isColorOn)
    {
        if (isColorOn)
        {
            spriteRenderer.material.color = isColorOn ? onColor : offColor;
        }
    }
}

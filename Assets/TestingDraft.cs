using UnityEngine;

public class DraftTester : MonoBehaviour
{
    public DraftPicker picker;
    public KeyCode triggerKey = KeyCode.F;

    void Reset()
    {
        if (picker == null) picker = FindFirstObjectByType<DraftPicker>();
    }

    void Update()
    {
        if (Input.GetKeyDown(triggerKey))
        {
            if (picker == null) picker = FindFirstObjectByType<DraftPicker>();
            if (picker == null) return;

            if (!picker.IsOpen())
                picker.StartDraft();
        }
    }
}

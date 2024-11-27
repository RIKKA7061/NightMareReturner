
using UnityEngine;

public class SpriteAnimation : MonoBehaviour
{
    [SerializeField] bool self_target;
    [SerializeField] SpriteRenderer sprite_target;
    [SerializeField] Sprite[] sprite_ani;
    [SerializeField] float timer;
    
    int reset_count = 0;

    private void Start()
    {
        if (sprite_ani.Length == 0)
        {
            enabled = false;
            return;
        }

        if (self_target)
        {
            sprite_target = GetComponent<SpriteRenderer>();
        }
        Invoke("OnTimerEnd", timer);
    }
        public void OnTimerEnd()
    {
        if (reset_count < sprite_ani.Length)
        {
            sprite_target.sprite = sprite_ani[reset_count];
            reset_count++;
        }
        else
        {
            reset_count = 0;
            sprite_target.sprite = sprite_ani[reset_count];
            reset_count++;
        }
        Invoke("OnTimerEnd", timer);
    }
}

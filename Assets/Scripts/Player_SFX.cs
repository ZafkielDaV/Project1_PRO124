using UnityEngine;

public class PlayerSFX : MonoBehaviour
{

    [Header("Attack / Dash / Hurt / Die")]
    public AudioClip attackClip;
    public AudioClip dashClip;
    public AudioClip hurtClip;
    public AudioClip dieClip;

    public void PlayAttack() { if (attackClip) sfxSource.PlayOneShot(attackClip); }
    public void PlayDash() { if (dashClip) sfxSource.PlayOneShot(dashClip); }
    public void PlayHurt() { if (hurtClip) sfxSource.PlayOneShot(hurtClip); }
    public void PlayDie() { if (dieClip) sfxSource.PlayOneShot(dieClip); }
    [Header("Audio Source")]
    public AudioSource sfxSource;

    [Header("Jump")]
    public AudioClip jumpClip;

    [Header("Footstep")]
    public AudioClip[] footstepClips;
    public float footstepInterval = 0.35f; // khoảng cách giữa 2 tiếng bước chân
    private float footstepTimer;

    public void PlayJump()
    {
        if (jumpClip == null) return;
        sfxSource.PlayOneShot(jumpClip);
    }

    public void PlayFootstep()
    {
        if (footstepClips.Length == 0) return;
        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
        sfxSource.PlayOneShot(clip);
    }

    // Gọi hàm này mỗi frame khi player đang chạy trên mặt đất
    public void HandleFootstepTimer(bool isMoving, bool isGrounded)
    {
        if (isMoving && isGrounded)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                PlayFootstep();
                footstepTimer = footstepInterval;
            }
        }
        else
        {
            footstepTimer = 0f; // reset để bước đầu tiên phát ngay khi đi tiếp
        }
    }
}
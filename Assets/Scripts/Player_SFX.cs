using UnityEngine;

/// <summary>
/// Quản lý toàn bộ SFX của Player (bước chân, nhảy, đánh, dash, hurt, chết).
/// Gắn script này cùng GameObject với Player.cs — Player.cs sẽ tự GetComponent và gọi các hàm Play... bên dưới.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class PlayerSFX : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;       // dùng chung cho jump / attack / dash / hurt / die
    [SerializeField] private AudioSource footstepSource;  // tách riêng để tiếng bước không bị các SFX khác cắt ngang

    [Header("Footstep")]
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private float footstepInterval = 0.35f; // khoảng cách giữa 2 bước khi đi bộ
    private float footstepTimer;

    [Header("Jump")]
    [SerializeField] private AudioClip[] jumpClips;

    [Header("Attack")]
    [SerializeField] private AudioClip[] attackClips;

    [Header("Dash")]
    [SerializeField] private AudioClip[] dashClips;

    [Header("Hurt")]
    [SerializeField] private AudioClip[] hurtClips;

    [Header("Die")]
    [SerializeField] private AudioClip dieClip;

    [Header("Tinh chỉnh âm lượng riêng từng loại")]
    [SerializeField][Range(0f, 1f)] private float masterVolume = 1f;   // nhân chung lên tất cả
    [SerializeField][Range(0f, 1f)] private float footstepVolume = 0.6f;
    [SerializeField][Range(0f, 1f)] private float jumpVolume = 0.8f;
    [SerializeField][Range(0f, 1f)] private float attackVolume = 0.9f;
    [SerializeField][Range(0f, 1f)] private float dashVolume = 0.8f;
    [SerializeField][Range(0f, 1f)] private float hurtVolume = 1f;
    [SerializeField][Range(0f, 1f)] private float dieVolume = 1f;

    [Header("Random Pitch (đỡ nghe lặp lại nhàm tai)")]
    [SerializeField] private bool randomizePitch = true;
    [SerializeField] private float pitchMin = 0.94f;
    [SerializeField] private float pitchMax = 1.06f;

    private void Awake()
    {
        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();
        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f; // Player là nguồn gần Listener nhất -> để 2D cho ổn định, khỏi bị hụt âm

        if (footstepSource == null)
            footstepSource = gameObject.AddComponent<AudioSource>();

        footstepSource.playOnAwake = false;
        footstepSource.loop = false;
        footstepSource.spatialBlend = 0f;
    }

    /// <summary>
    /// Gọi mỗi frame từ Player.Update(). isMoving: có đang giữ phím di chuyển không. isGrounded: có đang chạm đất không.
    /// </summary>
    public void HandleFootstepTimer(bool isMoving, bool isGrounded)
    {
        if (!isMoving || !isGrounded)
        {
            footstepTimer = 0f; // đứng lại / đang nhảy -> reset để lần đi tiếp theo phát tiếng ngay
            return;
        }

        footstepTimer -= Time.deltaTime;
        if (footstepTimer <= 0f)
        {
            footstepTimer = footstepInterval;
            PlayClip(footstepSource, footstepClips, footstepVolume);
        }
    }

    public void PlayJump() => PlayClip(sfxSource, jumpClips, jumpVolume);
    public void PlayAttack() => PlayClip(sfxSource, attackClips, attackVolume);
    public void PlayDash() => PlayClip(sfxSource, dashClips, dashVolume);
    public void PlayHurt() => PlayClip(sfxSource, hurtClips, hurtVolume);

    public void PlayDie()
    {
        if (dieClip == null || sfxSource == null) return;
        sfxSource.pitch = 1f; // tiếng chết giữ nguyên cao độ, không random
        sfxSource.PlayOneShot(dieClip, dieVolume * masterVolume);
    }

    private void PlayClip(AudioSource source, AudioClip[] clips, float volume)
    {
        if (source == null || clips == null || clips.Length == 0) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null) return;

        source.pitch = randomizePitch ? Random.Range(pitchMin, pitchMax) : 1f;
        source.PlayOneShot(clip, volume * masterVolume);
    }

    /// <summary>Đổi âm lượng tổng lúc runtime, ví dụ khi người chơi kéo thanh Volume trong Settings.</summary>
    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
    }
}
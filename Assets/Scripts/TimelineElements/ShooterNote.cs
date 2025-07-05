using DG.Tweening;
using UnityEngine;

public class ShooterNote : WarnNote
{
    public Transform shootPosition;
    public GameObject shootProjectile;
    private Tween projectileTween;

    public void Leave()
    {
        Debug.Log("Leaving");
        durationHandle.transform.DOKill();
        durationHandle.transform.DOMoveY(8f, 0.5f)
            .SetEase(Ease.InBack)
            .OnComplete(() => {
                gameObject.SetActive(false);
            });
    }

    protected override void Attack()
    {
        if (shootProjectile == null) return;
        if (defeated) return;

        durationHandle.transform.DOKill();
        shootProjectile.transform.DOKill();

        warning = false;
        attacked = true;
        durationHandle.transform.rotation = Quaternion.identity;
        SetAnimated(true);
        shootProjectile.gameObject.SetActive(true);
        shootProjectile.transform.position = shootPosition.position;
        float durationSeconds = attackDuration / 1000f;

        trail.emitting = true;
        float startX = shootProjectile.transform.position.x;
        float speed = startX / durationSeconds; // Units per second
        float totalDistance = startX + 10;
        float totalDuration = totalDistance / speed;

        projectileTween = shootProjectile.transform.DOMoveX(-10, totalDuration)
        .SetEase(Ease.Linear)
        .OnComplete(() => {
            trail.emitting = false;
        });
    }

    public override void Kill()
    {
        defeated = true;

        // Ensure the projectile is active and positioned before tweening
        shootProjectile.gameObject.SetActive(true);
        projectileTween.Kill();

        // Optional: force reset position if needed
        shootProjectile.transform.position = new Vector3(0f, shootProjectile.transform.position.y, 0f);

        float durationSeconds = attackDuration / 1000f;
        trail.emitting = true;

        // Animate back to the handle
        projectileTween = shootProjectile.transform.DOMoveX(durationHandle.transform.position.x, durationSeconds)
            .SetEase(Ease.Linear)
            .OnComplete(() => {
                Instantiate(killParticles, durationHandle.transform.position, Quaternion.identity);
                gameObject.SetActive(false);
            });
    }

    public override void SetDisplayMode(bool gameplay)
    {
        base.SetDisplayMode(gameplay);

        if (!gameplay) {
            shootProjectile.transform.DOKill();
            shootProjectile.gameObject.SetActive(false);
        }
    }
}

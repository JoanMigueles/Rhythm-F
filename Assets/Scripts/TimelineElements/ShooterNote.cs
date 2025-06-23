using DG.Tweening;
using UnityEngine;

public class ShooterNote : WarnNote
{
    public Transform shootPosition;
    public GameObject shootProjectile;
    private bool isBeingAttacked = false;

    public void Leave()
    {
        durationHandle.transform.DOMoveY(8f, 0.5f)
            .SetEase(Ease.InBack)
            .OnComplete(() => {
                gameObject.SetActive(false);
            });
    }


    protected override void Attack()
    {
        if (shootProjectile == null) return;

        warning = false;
        attacked = true;
        durationHandle.transform.rotation = Quaternion.identity;
        shootProjectile.gameObject.SetActive(true);
        shootProjectile.transform.position = shootPosition.position;
        float durationSeconds = attackDuration / 1000f;

        trail.emitting = true;
        float startX = shootProjectile.transform.position.x;
        float speed = startX / durationSeconds; // Units per second
        float totalDistance = startX + 10;
        float totalDuration = totalDistance / speed;

        attackTween = shootProjectile.transform.DOMoveX(-10, totalDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() => {
                attackTween = null;
                trail.emitting = false;
            });
    }

    public void ReturnBullet()
    {
        if (data.type != NoteType.Warn_Slash) return;

        isBeingAttacked = true;
        shootProjectile.transform.position = new Vector3(0f, shootProjectile.transform.position.y, 0f);
        float durationSeconds = attackDuration / 1000f;
        trail.emitting = true;
        attackTween = shootProjectile.transform.DOMoveX(durationHandle.transform.position.x, durationSeconds)
                .SetEase(Ease.Linear)
                .OnComplete(() => {
                    attackTween = null;
                    trail.emitting = false;
                    gameObject.SetActive(false);
                });
    }

    protected override void HideGhost(bool hide)
    {
        base.HideGhost(hide);
        shootProjectile.GetComponent<SpriteRenderer>().enabled = !hide;
    }


    public bool IsBeingAttacked() { return isBeingAttacked; }

    public override void SetDisplayMode(bool gameplay)
    {
        base.SetDisplayMode(gameplay);

        if (!gameplay) {
            shootProjectile.transform.DOKill();
            shootProjectile.gameObject.SetActive(false);
            isBeingAttacked = false;
        }
    }
}

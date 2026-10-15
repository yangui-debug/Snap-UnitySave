using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Anim.CampSelect
{
    public class MonsterPvpCampAnim : MonoBehaviour
    {
        [SerializeField] private Transform monsterPanel = default;
        [SerializeField] private Transform weaponPanel = default;
        [SerializeField] private List<Animator> monsters = default;
        [SerializeField] private List<Animator> weapons = default;
        [SerializeField] private SpriteRenderer mask = default;
        [SerializeField] private List<Image> masks = default;
        [SerializeField] private GameObject[] stars = default;
        [SerializeField] private GameObject btn = default;
        [SerializeField] private CanvasGroup monsterSide = default;
        [SerializeField] private CanvasGroup becomeMonster = default;
        [SerializeField] private CanvasGroup weaponSide = default;
        [SerializeField] private CanvasGroup becomeWeapon = default;

        private const int BackScale = 40;
        private const int StartScale = 55;
        private const int FrontScale = 75;
        private Vector3 monsterBackPos;
        private Vector3 monsterStartPos;
        private Vector3 monsterFrontPos;
        private Vector3 weaponBackPos;
        private Vector3 weaponStartPos;
        private Vector3 weaponFrontPos;
        private Sequence seq;
        private bool normal = true;

        public void Awake()
        {
            monsterBackPos = new Vector3(-130, 40, -500);
            monsterStartPos = new Vector3(25, -10, -500);
            monsterFrontPos = new Vector3(170, -360, -2000);
            weaponBackPos = new Vector3(190, 65, -500);
            weaponStartPos = new Vector3(110, 100, -500);
            weaponFrontPos = new Vector3(0, 780, -2000);
        }

        public void SelectMonster()
        {
            normal = false;
            monsterSide.DOFade(0, 0.1f);
            weaponSide.DOFade(0, 0.1f);
            becomeWeapon.DOFade(0, 0.1f);
            monsterPanel.DOLocalMove(monsterFrontPos, 0.5f);
            monsterPanel.DOScale(FrontScale, 0.5f);
            weaponPanel.DOLocalMove(weaponBackPos, 0.5f);
            weaponPanel.DOScale(BackScale, 0.5f);
            mask.DOFade(0f, 0.1f);
            masks.ForEach(x => x.DOFade(0f, 0.1f));

            seq?.Kill();
            seq = DOTween.Sequence();
            seq.AppendInterval(0.5f);
            seq.Append(mask.DOFade(0.6f, 0.1f));
            masks.ForEach(x => seq.Join(x.DOFade(0.6f, 0.1f)));
            seq.AppendCallback(() =>
            {
                becomeMonster.DOFade(1, 0.2f);
                btn.SetActive(true);
            });

            foreach (GameObject star in stars)
            {
                star.SetActive(false);
            }

            foreach (Animator monster in monsters)
            {
                monster.Play("Choose");
            }

            foreach (Animator weapon in weapons)
            {
                weapon.Play(normal ? "Idle" : "Cancel");
            }
        }

        public void SelectEquip()
        {
            normal = false;
            monsterSide.DOFade(0, 0.1f);
            weaponSide.DOFade(0, 0.1f);
            becomeMonster.DOFade(0, 0.1f);
            monsterPanel.DOLocalMove(monsterBackPos, 0.5f);
            monsterPanel.DOScale(BackScale, 0.5f);
            weaponPanel.DOLocalMove(weaponFrontPos, 0.5f);
            weaponPanel.DOScale(FrontScale, 0.5f);
            mask.DOFade(0f, 0.1f);
            masks.ForEach(x => x.DOFade(0f, 0.1f));

            seq?.Kill();
            seq = DOTween.Sequence();
            seq.AppendInterval(0.5f);
            seq.Append(mask.DOFade(0.6f, 0.1f));
            masks.ForEach(x => seq.Join(x.DOFade(0.6f, 0.1f)));
            seq.AppendCallback(() =>
            {
                becomeWeapon.DOFade(1, 0.2f);
                btn.SetActive(true);
            });

            foreach (GameObject star in stars)
            {
                star.SetActive(true);
            }

            foreach (Animator monster in monsters)
            {
                monster.Play(normal ? "Idle" : "Cancel");
            }

            foreach (Animator weapon in weapons)
            {
                weapon.Play("Choose");
            }
        }

        public void SetDefault()
        {
            normal = true;
            btn.SetActive(false);
            becomeMonster.DOFade(0, 0.1f);
            becomeWeapon.DOFade(0, 0.1f);
            monsterPanel.DOLocalMove(monsterStartPos, 0.5f);
            monsterPanel.DOScale(StartScale, 0.5f);
            weaponPanel.DOLocalMove(weaponStartPos, 0.5f);
            weaponPanel.DOScale(StartScale, 0.5f);
            mask.DOFade(0, 0.5f);
            masks.ForEach(x => x.DOFade(0, 0.5f));
            monsterSide.DOFade(1, 0.2f);
            weaponSide.DOFade(1, 0.2f);
            foreach (GameObject star in stars)
            {
                star.SetActive(true);
            }

            foreach (Animator monster in monsters)
            {
                monster.Play("Idle");
            }

            foreach (Animator weapon in weapons)
            {
                weapon.Play("Idle");
            }
        }
    }
}
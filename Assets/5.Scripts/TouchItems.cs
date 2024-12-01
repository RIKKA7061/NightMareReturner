using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;

public class TouchItems : MonoBehaviour
{
	[Header("�����۰� ������")]
	public GameObject portal;

	[Header("�̰� class��?")]
	public bool isClass;

	[Header("���ݷ� ����")]
	public int AddAtk = 0;  // ���ݷ� ����

	[Header("ü�� ȸ��")]
	public int Heal = 0;    // ü�� ȸ��

	[Header("�ִ� ü�� ����")]
	public int AddHp = 0;   // �ִ� ü�� ����

	[Header("��ȭ ����")]
	public int AddMoney = 0;// ��ȭ ����

	[Header("���� ȹ��")]
	public int Round = 0;   // ���� ȹ��
	private Player player;

	// �÷��̾� ��ũ��Ʈ
	private void Awake()
	{
		player = FindObjectOfType<Player>(); // ������ ����ߵ� (�ʱ�ȭ)
	}

	private void OnTriggerEnter2D(Collider2D other)// �÷��̾� �浹��
	{
		if (other.CompareTag("Player"))
		{
			player.Atk += AddAtk;       // ���ݷ� ����
			player.Atk2 = player.Atk - 25;
			player.nowHP += Heal;		// ü�� ȸ��
			player.maxHP += AddHp;      // �ִ�ü�� ����
			player.maxHP2 = player.maxHP - 500;
			Player.Money += AddMoney;	// ��ȭ ȹ��
			Player.round += Round;		// ���� ȹ��

			if (player.nowHP > player.maxHP) player.nowHP = player.maxHP;

			// Ŭ���� �������� ���
			if(isClass)
			{
				// Ȱ��ȭ
				portal.SetActive(true);

				// �� �ڽ� ��Ȱ��ȭ
				gameObject.SetActive(false);
			}
			else
			{
				Destroy(gameObject);        // �� �ڽ��� ������ ����
			}
		}
	}


}

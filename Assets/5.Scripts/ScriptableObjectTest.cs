using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Test Data", menuName = "Scriptable Object/Test Data", order = int.MaxValue)]
public class ScriptableObjectTest : ScriptableObject
{
	public enum ItemType
	{
		Item1,
		Item2,
		Item3
	}

	[SerializeField]
	private ItemType itemType;
	public ItemType Type { get { return itemType; } }


	[SerializeField]
    private string itemName;
    public string Name { get { return name; } }

    [SerializeField]
    private int num1;
    public int Num1 { get { return num1; } }

    [SerializeField]
    private float num2;
    public float Num2 { get { return num2; } }

    [SerializeField]
    private float num3;
    public float Num3 { get { return num3; } }
}

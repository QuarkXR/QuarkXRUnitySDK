using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Wave.Essence.Samples.WaveController
{
	public class clickHandle : MonoBehaviour
	{
		// Start is called before the first frame update
		void OnEnable()
		{
			GameObject bs = GameObject.Find("BackButton");
			if (bs != null)
			{
				bs.SetActive(false);
			}
		}

		// Update is called once per frame
		void Update()
		{

		}

		public void BackToUpLayer()
		{
			SceneManager.LoadScene(0);
		}

		public void ExitGame()
		{
			Application.Quit();
		}
	}
}

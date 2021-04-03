using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManagerr : MonoBehaviour 
{

	public Questionn[] questions;
	private static List<Questionn> unansweredQuestions;

	private Questionn currentQuestion;

	[SerializeField]
	private Text factText;

	[SerializeField]
	private Text trueAnswerText;

	[SerializeField]
	private Text falseAnswerText;

	[SerializeField]
	private float timeBetweenQuestions = 1f;

	[SerializeField]
	private Animator animator;

	void Start()
	{
		if (unansweredQuestions == null || unansweredQuestions.Count == 0) 
		{
			unansweredQuestions = questions.ToList<Questionn>(); 
		}

		SetCurrentQuestion ();

	}

	void SetCurrentQuestion()
	{
		int randomQuestionIndex = Random.Range (0, unansweredQuestions.Count);
		currentQuestion = unansweredQuestions [randomQuestionIndex];

		factText.text = currentQuestion.fact;

		//Debug.Log(currentQuestion.n);
		currentQuestion.obj.gameObject.SetActive(true);
		if (currentQuestion.isTrue) 
		{
			trueAnswerText.text = "CORRECTO";
			falseAnswerText.text = "INCORRECTO";
		}
		else
		{
			trueAnswerText.text = "INCORRECTO";
			falseAnswerText.text = "CORRECTO";
		}

	}

	IEnumerator TransitionToNextQuestion()
	{
		unansweredQuestions.Remove(currentQuestion);
		Debug.Log("a");
		//Debug.Log(currentQuestion.n);
		yield return new WaitForSeconds (timeBetweenQuestions);

		SceneManager.LoadScene (SceneManager.GetActiveScene ().buildIndex);
		Debug.Log("b");
	}

	public void UserSelectTrue()
	{
		animator.SetTrigger ("True");
		if (currentQuestion.isTrue) {
			Debug.Log ("CORRECTO!!");
		} 
		else 
		{
			Debug.Log ("INCORRECTO!!");
		}

		StartCoroutine (TransitionToNextQuestion ());
	}

	public void UserSelectFalse()
	{
		animator.SetTrigger ("False");
		if (!currentQuestion.isTrue) {
			Debug.Log ("CORRECTO!!");
		} 
		else 
		{
			Debug.Log ("INCORRECTO!!");
		}

		StartCoroutine (TransitionToNextQuestion ());
	}

}

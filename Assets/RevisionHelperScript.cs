using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using UnityEngine;
using Wawa.DDL;
using Wawa.Optionals;
using Rnd = UnityEngine.Random;

public class RevisionHelperScript : MonoBehaviour
{
    static int _moduleIdCounter = 1;
    int _moduleID = 0;

    public KMNeedyModule Module;
    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMSelectable ModuleSelectable;
    public KMSelectable[] Displays;
    public KMSelectable[] Arrows;
    public TextMesh[] Texts;
    public TextMesh TestText;
    public Material BG;
    public SpriteRenderer Dot;
    public GameObject[] Hinges;

    private static readonly Vector3[] HingePositions = new[]
    {
        Vector3.left,
        Vector3.right,
        Vector3.forward,
        Vector3.zero,
        Vector3.back,
        Vector3.left,
        Vector3.zero,
        Vector3.right,
        Vector3.forward,
        Vector3.zero,
        Vector3.back
    };
    private const float HingeExtension = 0.0075f;
    private const float AnswerBounds = 20 / 3f;

    private static readonly KeyCode[] TypableKeys =
    {
        KeyCode.Return,
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3,
        KeyCode.UpArrow, KeyCode.DownArrow,
        KeyCode.W, KeyCode.S
    };

    private Coroutine FlashCoroutine, RainbowCoroutine;
    private int ChosenQuestion, CorrectButton, Page, PageCount, QuestionSect;
    private const float AnswerWidth = 0.0525f;
    private const float AnswerMultiplier = 1.125f;
    private List<List<string>> BackupQuestions = new List<List<string>>() { new List<string>() { "No questions created. Go into this module's mod settings and add some!", "Correct answer", "Incorrect answer #1", "Incorrect answer #2" } };
    private List<List<string>> TestQuestions = new List<List<string>>() { /*new List<string>() { @"❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽haaaaaaaaaaaaaaaaaaaaaaa❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽❽", @"<tff>" },
    new List<string>() { "woah it's a question", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18" },*/
    new List<string>() { "ass?", "probably", "NO WAY YOU ABSOLUTE LOSER WHY WOULD ASS. HNNNNNNNNNNNNNNNNNGGGGGGGGGG.", "NO WAY YOU ABSOLUTE LOSER WHY WOULD ASS. HNNNNNNNNNNNNNNNNNGGGGGGGG.", "NO WAY YOU ABSOLUTE LOSER WHY WOULD ASS. HNNNNNNNNNNNNNNNNNGGGGGG.", "NO WAY YOU ABSOLUTE LOSER WHY WOULD ASS. HNNNNNNNNNNNNNNNNNGGGG.", "NO WAY YOU ABSOLUTE LOSER WHY WOULD ASS. HNNNNNNNNNNNNNNNNNGG.", "NO WAY YOU ABSOLUTE LOSER WHY WOULD ASS. HNNNNNNNNNNNNNNNNN.", "NO WAY YOU ABSOLUTE LOSER WHY WOULD ASS. HNNNNNNNNNNNNNNN.", "NO WAY YOU ABSOLUTE LOSER WHY WOULD ASS. HNNNNNNNNNNNNN.", "NO WAY YOU ABSOLUTE LOSER WHY WOULD ASS. HNNNNNNNNNNN.", "NO WAY YOU ABSOLUTE LOSER WHY WOULD ASS. HNNNNNNNNN.", "NO WAY YOU ABSOLUTE LOSER WHY WOULD ASS. HNNNNNNN.", "NO WAY YOU ABSOLUTE LOSER WHY WOULD ASS. HNNNNN." }
    };
    public List<List<string>> MissionQuestions = new List<List<string>>();
    private List<string> ShuffledAnswers = new List<string>();
    private List<string> QuestionDisplay = new List<string>();
    private List<string> ChosenQuList = new List<string>();
    private float FlashMultiplier = 1f, MissionBonusTime, MissionLongIncrement, MissionOverThreeIncrement, MissionCountdown;
    private string LogAnswers, LogQuestion;
    private bool[] ValidAnswers = new bool[3], MissionInfo = new bool[5];
    private bool Active, Backup, Editor, Focused, HasActivated, TrueFalse;
    private Settings _Settings = new Settings();
    private List<SpriteRenderer> Dots = new List<SpriteRenderer>();

    private const float BackupCD = 45f;
    private const float BackupOTAI = 2f;
    private const float BackupLQI = 2f;
    private const float BackupTPBT = 15f;
    class Settings
    {
        public List<List<string>> Questions;
        public float BaseCountdownTime = 45f;
        public float OverThreeAnswersIncrement = 2f;
        public float LongQuestionIncrement = 2f;
        public float TwitchPlaysBonusTime = 15f;
    }

    void GetSettings()
    {
        var SettingsConfig = new ModConfig<Settings>("RevisionHelper");
        _Settings = SettingsConfig.Settings; // This reads the settings from the file, or creates a new file if it does not exist
        SettingsConfig.Settings = _Settings; // This writes any updates or fixes if there's an issue with the file
    }

    void SortOutMissionDescription()
    {
        try
        {
            string description = Application.isEditor ? "" : Missions.Description.UnwrapOr("[Revision Helper] act 30\n[Revision Helper] bon 3\n[Revision Helper] 3an 50\n[Revision Helper] lon 8\n[Revision Helper] question  correct  incorrect");
            var matches = Regex.Matches(description, @"^(?:// )?\[Revision ?Helper\] (.*)$", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            for (int i = 0; i < matches.Count; i++)
            {
                if (matches[i].Groups[1].Value.ToLowerInvariant().Substring(0, 4) == "3an ")
                {
                    try
                    {
                        MissionOverThreeIncrement = float.Parse(matches[i].Groups[1].Value.Substring(4, matches[i].Groups[1].Value.Length - 4));
                        MissionInfo[1] = true;
                    }
                    catch
                    {
                        Debug.LogFormat("[Revision Helper #{0}] Could not assign a new value for the over 3 answer increment: ignoring line.", _moduleID);
                    }
                }
                else if (matches[i].Groups[1].Value.ToLowerInvariant().Substring(0, 4) == "act ")
                {
                    try
                    {
                        MissionCountdown = float.Parse(matches[i].Groups[1].Value.Substring(4, matches[i].Groups[1].Value.Length - 4));
                        MissionInfo[2] = true;
                    }
                    catch
                    {
                        Debug.LogFormat("[Revision Helper #{0}] Could not assign a new value for the base countdown time: ignoring line.", _moduleID);
                    }
                }
                else if (matches[i].Groups[1].Value.ToLowerInvariant().Substring(0, 4) == "bon ")
                {
                    try
                    {
                        MissionBonusTime = float.Parse(matches[i].Groups[1].Value.Substring(4, matches[i].Groups[1].Value.Length - 4));
                        MissionInfo[3] = true;
                    }
                    catch
                    {
                        Debug.LogFormat("[Revision Helper #{0}] Could not assign a new value for the Twitch Plays countdown bonus: ignoring line.", _moduleID);
                    }
                }
                else if (matches[i].Groups[1].Value.ToLowerInvariant().Substring(0, 4) == "lon ")
                {
                    try
                    {
                        MissionLongIncrement = float.Parse(matches[i].Groups[1].Value.Substring(4, matches[i].Groups[1].Value.Length - 4));
                        MissionInfo[4] = true;
                    }
                    catch
                    {
                        Debug.LogFormat("[Revision Helper #{0}] Could not assign a new value for the long question increment: ignoring line.", _moduleID);
                    }
                }
                if (!new[] { "3an ", "act ", "bon ", "lon " }.Contains(matches[i].Groups[1].Value.ToLowerInvariant().Substring(0, 4)))
                {
                    var temp = matches[i].Groups[1].Value.Split(new string[] { "   " }, StringSplitOptions.None).Select((x, ix) => x.Replace("\r", "").Split(new string[] { "  " }, StringSplitOptions.None).ToList());
                    foreach (var item in temp)
                        MissionQuestions.Add(item);
                    MissionInfo[0] = true;
                }
            }
        }
        catch (Exception e)
        {
            Error(e);
        }
    }

    void Awake()
    {
        try
        {
            _moduleID = _moduleIdCounter++;
            GetSettings();
            if (_Settings.Questions == null)
                _Settings.Questions = new List<List<string>>() { new List<string>() { "No questions created. Go into this module's mod settings and add some!", "Correct answer", "Incorrect answer #1", "Incorrect answer #2" } };
            SortOutMissionDescription();
            RainbowCoroutine = StartCoroutine(Rainbow());
            Dot.color = new Color();
            BG.color = new Color(0, 1, 1);
            Texts[4].text = "";
            for (int i = 0; i < Displays.Length; i++)
            {
                int x = i;
                Displays[i].OnInteract += delegate { ButtonPress(x); return false; };
            }
            for (int i = 0; i < Arrows.Length; i++)
            {
                int x = i;
                Arrows[i].OnInteract += delegate { ArrowPress(x); return false; };
            }
            for (int i = 0; i < 4; i++)
                Texts[i].text = "";
            Module.OnNeedyActivation += delegate { Activate(); };
            Module.OnTimerExpired += delegate { Strike(-1); };
            ModuleSelectable.OnFocus += delegate { Focused = true; };
            ModuleSelectable.OnDefocus += delegate { Focused = false; };
            for (int i = 0; i < Hinges.Length; i++)
                Hinges[i].transform.localPosition = HingePositions[i] * HingeExtension;
        }
        catch (Exception e)
        {
            Error(e);
        }
    }

    void Update()
    {
        for (int i = 0; i < TypableKeys.Count(); i++)
            if (Input.GetKeyDown(TypableKeys[i]) && Focused)
            {
                if (i < 4)
                    Displays[i].OnInteract();
                else
                    Arrows[i % 2].OnInteract();
            }
    }

    void Activate()
    {
        try
        {
            HasActivated = true;
            Page = 0;
            QuestionSect = 0;
            for (int i = 0; i < Dots.Count; i++)
                Destroy(Dots[i].gameObject);
            Dots = new List<SpriteRenderer>();
            TrueFalse = false;
            for (int i = 1; i < 4; i++)
                Texts[i].color = new Color(1, 1, 1);
            for (int i = 1; i < 4; i++)
                Displays[i].GetComponent<MeshRenderer>().material.color = new Color(0, 0, 0);
            Backup = false;
            Editor = false;
            if (Application.isEditor)
                Editor = true;
            else if (_Settings.Questions.Where(x => x.Count() > 1).Count() == 0)
                Backup = true;
            ChosenQuestion = Rnd.Range(0, (MissionInfo[0] ? MissionQuestions : Editor ? TestQuestions : Backup ? BackupQuestions : _Settings.Questions).Count);
            ChosenQuList = (MissionInfo[0] ? MissionQuestions : Editor ? TestQuestions : Backup ? BackupQuestions : _Settings.Questions)[ChosenQuestion].ToList();
            PageCount = ((ChosenQuList.Count - 2) / 3) + 1;
            List<string> temp = ChosenQuList.ToList();
            LogQuestion = temp[0];
            Debug.LogFormat("[Revision Helper #{0}] Asking question #{1}: {2}", _moduleID, ChosenQuestion + 1, LogQuestion);
            temp.RemoveAt(0);
            while (temp.Count > 117)
                temp.RemoveAt(Rnd.Range(1, temp.Count));
            if (temp[0].RegexMatch("<tf[tf]>") && temp.Count == 1)
            {
                TrueFalse = true;
                CorrectButton = "tf".IndexOf(temp[0][3]) + 1;
                for (int i = 1; i < 4; i++)
                {
                    Texts[i].text = new string[] { "True", "False", "-----" }[i - 1];
                    Texts[i].color = new Color[] { new Color(0, 1, 0), new Color(1, 0, 0), new Color(0.5f, 0.5f, 0.5f) }[i - 1];
                }
                ValidAnswers = new bool[] { true, true, false };
                Debug.LogFormat("[Revision Helper #{0}] This question is a true/false question.", _moduleID);
                Debug.LogFormat("[Revision Helper #{0}] The correct answer to this question is {1}.", _moduleID, new string[] { "true", "false" }[CorrectButton - 1]);
            }
            else
            {
                List<int> tempNums = new List<int>(new int[temp.Count + 1]);
                for (int i = 0; i < temp.Count; i++)
                    for (int j = i + 1; j < temp.Count; j++)
                        if (temp[i] == temp[j])
                            tempNums[j]++;
                for (int i = 1; i < temp.Count + 1; i++)
                    if (tempNums[i] != 0)
                        temp[i] = temp[i] + " (" + tempNums[i] + ")";
                if (PageCount > 1)
                    InstantiateDots(PageCount);
                temp.Shuffle();
                ShuffledAnswers = temp.ToList();
                for (int i = 1; i < 4; i++)
                {
                    try
                    {
                        Texts[i].text = temp[i - 1].Select(x => TestText.font.HasCharacter(x) ? x.ToString() : "◊").Join("");
                        ValidAnswers[i - 1] = true;
                    }
                    catch
                    {
                        Texts[i].text = "-----";
                        Texts[i].color = new Color(0.5f, 0.5f, 0.5f);
                        ValidAnswers[i - 1] = false;
                    }
                }
                CorrectButton = temp.IndexOf(ChosenQuList[1]) + 1;
                Debug.LogFormat("[Revision Helper #{0}] The answers: {1}.", _moduleID, temp.Join(", "));
                Debug.LogFormat("[Revision Helper #{0}] The correct answer to this question is answer #{1}: {2}.", _moduleID, CorrectButton, ChosenQuList[1]);
            }
            List<List<string>> temp2 = new List<List<string>>();
            for (int i = 0; i < ((temp.Count - 1) / 3) + 1; i++)
            {
                temp2.Add(new List<string>());
                for (int j = 0; j < 3; j++)
                {
                    try
                    {
                        temp2[i].Add(temp[(i * 3) + j]);
                    }
                    catch { }
                }
            }
            LogAnswers = temp2.Select(x => "[\"" + x.Join("\", \"") + "\"]").Join(", ");
            Active = true;
            for (int i = 1; i < 4; i++)
            {
                Texts[i].transform.localScale = new Vector3(AnswerBounds, Texts[i].transform.localScale.y, Texts[i].transform.localScale.z);
                float scale = Mathf.Min((AnswerWidth / Texts[i].GetComponent<Renderer>().bounds.extents.x) * AnswerMultiplier, 1f);
                Texts[i].transform.localScale = new Vector3(scale * AnswerBounds, Texts[i].transform.localScale.y, Texts[i].transform.localScale.z);
            }
            string temp3 = ChosenQuList[0].Select(x => TestText.font.HasCharacter(x) ? x.ToString() : "◊").Join("").Wrap(26);
            QuestionDisplay = temp3.Split('\n').ToList();
            Texts[0].text = QuestionDisplay.GetRange(0, Mathf.Min(QuestionDisplay.Count, 4)).Join("\n");
            Arrows[0].GetComponent<MeshRenderer>().material.color = new Color();
            try
            {
                Module.SetNeedyTimeRemaining((MissionInfo[2] ? MissionCountdown : _Settings.BaseCountdownTime) + (TwitchPlaysActive ? (MissionInfo[3] ? MissionBonusTime : _Settings.TwitchPlaysBonusTime) : 0) + (Mathf.Max(QuestionDisplay.Count - 4, 0) * (MissionInfo[4] ? MissionLongIncrement : _Settings.LongQuestionIncrement)) + ((PageCount - 1) * (MissionInfo[1] ? MissionOverThreeIncrement : _Settings.OverThreeAnswersIncrement)));
            }
            catch
            {
                try
                {
                    Module.SetNeedyTimeRemaining(BackupCD + (TwitchPlaysActive ? BackupTPBT : 0) + ((QuestionDisplay.Count - 4) * BackupLQI) + (PageCount * BackupOTAI));
                }
                catch (Exception e)
                {
                    Error(e);
                }
            }
            if (QuestionDisplay.Count > 4)
                Arrows[1].GetComponent<MeshRenderer>().material.color = new Color(0.5f, 0.5f, 0.5f);
            else
                Arrows[1].GetComponent<MeshRenderer>().material.color = new Color();
            StartCoroutine(Contract());
            FlashCoroutine = StartCoroutine(PerformFlashes());
        }
        catch (Exception e)
        {
            Error(e);
        }
    }

    void InstantiateDots(int count)
    {
        try
        {
            for (int i = 0; i < count; i++)
            {
                Dots.Add(Instantiate(original: Dot, parent: Dot.transform.parent));
                Dots[0].transform.localRotation = Dot.transform.localRotation;
                Dots[0].transform.localPosition = Dot.transform.localPosition;
                Dots[i].color = new Color(1, 1, 1, i == 0 ? 1 : 0.5f);
            }
            Dots[0].transform.localPosition = new Vector3(-0.0125f * (count - 1), Dots[0].transform.localPosition.y, Dots[0].transform.localPosition.z);
            for (int i = 1; i < count; i++)
                Dots[i].transform.localPosition = new Vector3((-0.0125f * (count - 1)) + (i * 0.025f), Dots[i].transform.localPosition.y, Dots[i].transform.localPosition.z);
        }
        catch (Exception e)
        {
            Error(e);
        }
    }

    void Error(Exception e)
    {
        try { StopCoroutine(RainbowCoroutine); } catch { }
        BG.color = new Color(1, 0.25f, 0.25f);
        Texts[4].text = "An error has\noccurred. Please\ncheck the log\nfor instructions.";
        Debug.LogFormat("[Revision Helper #{0}] An error has occured. Please report this error to GhostSalt #0217 on Discord. The error:", _moduleID);
        Debug.LogFormat("[Revision Helper #{0}] {1}", _moduleID, e);
        Debug.LogFormat("[Revision Helper #{0}] If the error was not shown entirely, check the filtered log.", _moduleID);
    }

    void ButtonPress(int pos)
    {
        try
        {
            Audio.PlaySoundAtTransform("press", Displays[pos].transform);
            StartCoroutine(ButtonAnim(pos, Displays));
            Displays[pos].AddInteractionPunch(0.5f);
            if (Active)
            {
                if (pos != 0 && (ValidAnswers[pos - 1]))
                {
                    if (pos == ((CorrectButton + 2) % 3) + 1 && Page == (CorrectButton - 1) / 3)
                    {
                        Module.HandlePass();
                        StartCoroutine(Expand());
                        Audio.PlaySoundAtTransform("correct", Displays[pos].transform);
                        Debug.LogFormat("[Revision Helper #{0}] The correct answer was submitted.", _moduleID);
                        Active = false;
                        Displays[((CorrectButton + 2) % 3) + 1].GetComponent<MeshRenderer>().material.color = TrueFalse && CorrectButton == 2 ? new Color(0.75f, 0, 0) : new Color(0, 0.75f, 0);
                        Texts[((CorrectButton + 2) % 3) + 1].color = new Color(0, 0, 0, 1);
                    }
                    else
                        Strike(pos);
                }
                else if (pos == 0 && ChosenQuList.Count > 4)
                {
                    Audio.PlaySoundAtTransform("page", Displays[pos].transform);
                    Page++;
                    Page %= PageCount;
                    for (int i = 0; i < Dots.Count; i++)
                        Dots[i].color = new Color(0.5f, 0.5f, 0.5f);
                    Dots[Page].color = new Color(1, 1, 1);
                    for (int i = 1; i < 4; i++)
                    {
                        try
                        {
                            Texts[i].text = ShuffledAnswers.Select(x => x.Select(y => TestText.font.HasCharacter(y) ? y.ToString() : "◊").Join("")).ToList()[(Page * 3) + i - 1];
                            Texts[i].color = new Color(1, 1, 1);
                            ValidAnswers[i - 1] = true;
                        }
                        catch
                        {
                            Texts[i].text = "-----";
                            Texts[i].color = new Color(0.5f, 0.5f, 0.5f);
                            ValidAnswers[i - 1] = false;
                        }
                        Texts[i].transform.localScale = new Vector3(AnswerBounds, Texts[i].transform.localScale.y, Texts[i].transform.localScale.z);
                        float scale = Mathf.Min((AnswerWidth / Texts[i].GetComponent<Renderer>().bounds.extents.x) * AnswerMultiplier, 1f);
                        Texts[i].transform.localScale = new Vector3(scale * AnswerBounds, Texts[i].transform.localScale.y, Texts[i].transform.localScale.z);
                    }
                }
                else
                    Audio.PlaySoundAtTransform("buzzer", Displays[pos].transform);
            }
            else
                Audio.PlaySoundAtTransform("buzzer", Displays[pos].transform);
        }
        catch (Exception e)
        {
            Error(e);
        }
    }

    void ArrowPress(int pos)
    {
        try
        {
            Audio.PlaySoundAtTransform("press", Arrows[pos].transform);
            StartCoroutine(ButtonAnim(pos, Arrows, 0.002f));
            Arrows[pos].AddInteractionPunch(0.5f);
            if (HasActivated)
            {
                if (QuestionDisplay.Count > 4 && !(QuestionSect <= 0 && pos == 0) && !(QuestionSect >= QuestionDisplay.Count - 4 && pos == 1))
                {
                    QuestionSect += pos == 0 ? -1 : 1;
                    if (QuestionSect == 0)
                        Arrows[0].GetComponent<MeshRenderer>().material.color = new Color();
                    else
                        Arrows[0].GetComponent<MeshRenderer>().material.color = new Color(0.5f, 0.5f, 0.5f);
                    if (QuestionSect + 4 == QuestionDisplay.Count)
                        Arrows[1].GetComponent<MeshRenderer>().material.color = new Color();
                    else
                        Arrows[1].GetComponent<MeshRenderer>().material.color = new Color(0.5f, 0.5f, 0.5f);
                    Texts[0].text = QuestionDisplay.Select(x => x.Select(y => TestText.font.HasCharacter(y) ? y.ToString() : "◊").Join("")).ToList().GetRange(QuestionSect, 4).Join("\n");
                }
                else
                    Audio.PlaySoundAtTransform("buzzer", Arrows[pos].transform);
            }
            else
                Audio.PlaySoundAtTransform("buzzer", Arrows[pos].transform);
        }
        catch (Exception e)
        {
            Error(e);
        }
    }

    void Strike(int pos)
    {
        try
        {
            Module.HandleStrike();
            Module.HandlePass();
            StartCoroutine(Expand());
            if (pos != -1)
                Debug.LogFormat("[Revision Helper #{0}] You submitted \"{1}\", which was incorrect. Strike!", _moduleID, TrueFalse ? new string[] { "true", "false" }[pos - 1] : ShuffledAnswers[(Page * 3) + pos - 1]);
            else
                Debug.LogFormat("[Revision Helper #{0}] You ran out of time. Strike!", _moduleID);
            for (int i = 0; i < Dots.Count; i++)
                Dots[i].color = new Color(0.5f, 0.5f, 0.5f);
            if (Dots.Count > 1)
                Dots[(CorrectButton - 1) / 3].color = new Color(1, 1, 1);
            for (int i = 1; i < 4; i++)
            {
                try
                {
                    Texts[i].text = TrueFalse ? new string[] { "True", "False", "-----" }[i - 1] : ShuffledAnswers[((CorrectButton - 1) / 3 * 3) + i - 1];
                    Texts[i].color = TrueFalse ? new Color[] { new Color(0, 1, 0), new Color(1, 0, 0), new Color(0.5f, 0.5f, 0.5f) }[i - 1] : new Color(1, 1, 1);
                }
                catch
                {
                    Texts[i].text = "-----";
                    Texts[i].color = new Color(0.5f, 0.5f, 0.5f);
                }
                Texts[i].transform.localScale = new Vector3(AnswerBounds, Texts[i].transform.localScale.y, Texts[i].transform.localScale.z);
                float scale = Mathf.Min((AnswerWidth / Texts[i].GetComponent<Renderer>().bounds.extents.x) * AnswerMultiplier, 1f);
                Texts[i].transform.localScale = new Vector3(scale * AnswerBounds, Texts[i].transform.localScale.y, Texts[i].transform.localScale.z);
            }
            Displays[((CorrectButton + 2) % 3) + 1].GetComponent<MeshRenderer>().material.color = TrueFalse && CorrectButton == 2 ? new Color(0.75f, 0, 0) : new Color(0, 0.75f, 0);
            Texts[((CorrectButton + 2) % 3) + 1].color = new Color(0, 0, 0, 1);
            Audio.PlaySoundAtTransform("strike", Displays[0].transform);
            Active = false;
        }
        catch (Exception e)
        {
            Error(e);
        }
    }

    private IEnumerator PerformFlashes(float intraInterval = 0.3f, float exoInterval = 0.9f)
    {
        while (true)
        {
            StartCoroutine(Flash());
            float timer = 0;
            while (timer < intraInterval)
            {
                yield return null;
                timer += Time.deltaTime;
            }
            StartCoroutine(Flash());
            timer = 0;
            while (timer < exoInterval)
            {
                yield return null;
                timer += Time.deltaTime;
            }
        }
    }

    private IEnumerator Flash(float duration = 0.25f, float start = 1.5f)
    {
        float timer = 0;
        while (timer < duration)
        {
            yield return null;
            timer += Time.deltaTime;
            FlashMultiplier = Easing.OutSine(timer, start, 1, duration);
        }
        FlashMultiplier = 1;
    }

    private IEnumerator Expand(float duration = 0.1f)
    {
        Audio.PlaySoundAtTransform("deactivate " + Rnd.Range(1, 6).ToString(), Module.transform);
        if (FlashCoroutine != null)
            StopCoroutine(FlashCoroutine);
        float timer = 0;
        while (timer < duration)
        {
            yield return null;
            timer += Time.deltaTime;
            for (int i = 0; i < Hinges.Length; i++)
                Hinges[i].transform.localPosition = Vector3.Lerp(-HingePositions[i], HingePositions[i], timer / duration) * HingeExtension;
        }
        for (int i = 0; i < Hinges.Length; i++)
            Hinges[i].transform.localPosition = HingePositions[i] * HingeExtension;
    }

    private IEnumerator Contract(float duration = 0.05f)
    {
        Audio.PlaySoundAtTransform("activate", Module.transform);
        float timer = 0;
        while (timer < duration)
        {
            yield return null;
            timer += Time.deltaTime;
            for (int i = 0; i < Hinges.Length; i++)
                Hinges[i].transform.localPosition = Vector3.Lerp(HingePositions[i], -HingePositions[i], timer / duration) * HingeExtension;
        }
        for (int i = 0; i < Hinges.Length; i++)
            Hinges[i].transform.localPosition = -HingePositions[i] * HingeExtension;
    }

    private IEnumerator ButtonAnim(int pos, KMSelectable[] buttons, float intensity = 0.006f)
    {
        for (int i = 0; i < 3; i++)
        {
            buttons[pos].transform.localPosition -= new Vector3(0, intensity / 3, 0);
            yield return null;
        }
        for (int i = 0; i < 3; i++)
        {
            buttons[pos].transform.localPosition += new Vector3(0, intensity / 3, 0);
            yield return null;
        }
    }

    private IEnumerator Rainbow(float duration = 25f, float sat = 0.75f, float val = 0.75f)
    {
        float timer = 0;
        while (true)
        {
            while (timer < duration)
            {
                yield return null;
                timer += Time.deltaTime * (Active ? 4f : 1f);
                BG.color = Color.HSVToRGB(Mathf.Lerp(0, 1f, timer / duration), sat, val) * FlashMultiplier;
            }
            timer = 0;
        }
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "Use '!{0} pdu123' to view the next page of answers, press the down arrow and the up arrow, then press answers 1-3. Use '!{0} cycle [1.5]' to cycle through the answers, optionally adding a number to the end as the delay between page flips, being a number between 0.1 and 5 and defaulting to 1.5 seconds. Delays can be written like \".5\". Use '!{0} question' to receive the question in chat and use '!{0} answers' to receive the answers in chat. \"question\" and \"answers\" can be abbreviated as \"q\" and \"a\".";
#pragma warning restore 414
    private bool TwitchPlaysActive;

    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant();
        string validcmds = "pdu123";
        if (!command.RegexMatch(@"^cycle( \d*(\.\d)?)?$") && command != "question" && command != "q" && command != "answers" && command != "a")
            for (int i = 0; i < command.Length; i++)
            {
                if (!validcmds.Contains(command[i]))
                {
                    yield return "sendtochaterror Invalid command.";
                    yield break;
                }
            }
        else if (command.RegexMatch(@"^cycle( \d*(\.\d)?)?$"))
        {
            float outFloat = 0;
            float delay = 0;
            yield return null;
            if (Active)
            {
                if (command.Length != 5)
                {
                    if (float.TryParse(command.Substring(6, command.Length - 6), out outFloat))
                    {
                        if (float.Parse(command.Substring(6, command.Length - 6)) <= 5f && float.Parse(command.Substring(6, command.Length - 6)) >= 0.1f)
                            delay = float.Parse(command.Substring(6, command.Length - 6));
                        else
                        {
                            delay = 1.5f;
                            yield return "sendtochaterror The given delay is out of range, defaulting to 1.5 seconds.";
                        }
                    }
                    else
                    {
                        delay = 1.5f;
                        yield return "sendtochaterror Invalid delay, defaulting to 1.5 seconds.";
                    }
                }
                else
                    delay = 1.5f;
                for (int i = 0; i < (ChosenQuList.Count - 1) / 3; i++)
                {
                    Displays[0].OnInteract();
                    if (i != ((ChosenQuList.Count - 1) / 3) - 1)
                    {
                        float timer = 0;
                        while (timer < delay)
                        {
                            yield return null;
                            timer += Time.deltaTime;
                        }
                    }
                }
            }
            else
                yield return "sendtochaterror Cannot cycle: the module is not active.";
            yield break;
        }
        else if (command == "question" || command == "q")
        {
            if (HasActivated)
                yield return "sendtochat The question " + (Active ? "is" : "was") + ": \"" + LogQuestion + "\"";
            else
                yield return "sendtochaterror Cannot send the question into the chat: the module has not generated any questions yet.";
            yield break;
        }
        else
        {
            if (HasActivated && !TrueFalse)
                yield return "sendtochat The answers " + (Active ? "are" : "were") + ": " + LogAnswers + ".";
            else if (HasActivated)
                yield return "sendtochat This is a true/false question: the answers are True and False.";
            else
                yield return "sendtochaterror Cannot send the answers into the chat: the module has not generated any answers yet.";
            yield break;
        }
        yield return null;
        for (int i = 0; i < command.Length; i++)
        {
            if (command[i] == 'u' || command[i] == 'd')
                Arrows["ud".IndexOf(command[i])].OnInteract();
            else
                Displays["p123".IndexOf(command[i])].OnInteract();
            if (i != command.Length - 1)
            {
                float timer = 0;
                while (timer < 0.15f)
                {
                    yield return null;
                    timer += Time.deltaTime;
                }
            }
        }
    }
}

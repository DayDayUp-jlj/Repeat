using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;
using System.Linq;

public class GameControl : MonoBehaviour
{
    [Header("Moving Settings")]
    public Transform[] anglesGroup = new Transform[5];
    public Transform startPos;
    public Transform endPos;
    private float xSpeed; //x轴速度
    public int[] speedGroup; //x轴的三个速度
    private float currentXAngle;
    private float currentYAngle;
    private float currentZAngle; //三个方向夹角
    private int countGame = 1;
    private bool isBallMoving = true;  //小球是否移动
    private bool isTriggerEnd = false; //小球是否触碰到重点墙
    private Vector3 currentSpeedVector;

    [Header("Visual Settings")]
    public Material visualMat;
    public Material invisualMat;
    private Renderer ballRenderer;

    [Header("RunShowing Settings")]
    public float invisualForwardTime = 1f; //变成不可见材质前的时间
    public float invisualForwardDistance = 10f; //变成不可见材质前的时间
    public bool timeOrDistance;
    public float resultDisplayTime = 3f; // 结果显示时间

    [Header("DataShowing Setting")]
    public GameObject resultBrand;
    public Text result;

    [Header("Delay Settings")]
    public float delayBeforeStop = 1f;  //每次试验后的延迟时间(s)
    private bool isDelayingStop = false;  //触碰到墙后的延迟

    [Header("Data Logging")]
    public string dataFileName; //excel文件名
    private string dataFilePath;

    [Header("RestTime Logging")]
    public float restTime = 30f; //休息时间（秒）
    public GameObject startBtnBrand;
    public Text restContent;
    public Button startBtn;
    private bool waitingForRest = false;  //等待休息按钮点击

    private List<(Vector3 SpeedVector, int speed)> ExperimentCombinations = new List<(Vector3, int)>(); //存储15组数据的列表 currentSpeedVector为当前的方向向量 speed为x轴的速度
    private int currentCombinationIndex = 0;
    private int[] currentRepetition = new int[15];

    private void Start()
    {
        //设置为60帧
        Application.targetFrameRate = 60;
        //创建文本文件
        dataFilePath = Path.Combine(Application.persistentDataPath, dataFileName);
        if (!File.Exists(dataFilePath))
        {
            File.AppendAllText(dataFilePath, "trail, angle_x(deg), angle_y(deg), angle_z(deg), speed(m/s), distance(m)\n");
        }

        //将实验进行分组
        foreach (Transform angleTransform in anglesGroup)
        {
            Vector3 SpeedVector = (angleTransform.position - startPos.position).normalized;

            foreach (int speed in speedGroup)
            {
                ExperimentCombinations.Add((SpeedVector, speed));
            }
        }

        for (int i = 0; i < ExperimentCombinations.Count; i++)
        {
            currentRepetition[i] = 10;
        }
        //将分好的组进行随机排序
        ShuffleExperimentOrder();

        ballRenderer = GetComponent<Renderer>();
        startBtnBrand.SetActive(false);
        startBtn.onClick.AddListener(OnStartButtonClick);

        //游戏运行逻辑
        StartCoroutine(RunningSequence());
    }

    /// <summary>
    /// 先实现练习，在实现主实验
    /// </summary>
    /// <returns></returns>
    IEnumerator RunningSequence()
    {
        File.AppendAllText(dataFilePath, "practiceData\n");
        yield return StartCoroutine(TryTrial());

        //小球位置重置
        transform.position = startPos.position;
        ballRenderer.material = visualMat;
        isBallMoving = true;
        isTriggerEnd = false;
        isDelayingStop = false;

        restContent.text = "练习结束";
        countGame = 1;
        currentCombinationIndex = Random.Range(0, 15);
        startBtnBrand.SetActive(true);
        waitingForRest = true;
        yield return new WaitUntil(() => !waitingForRest);
        startBtnBrand.SetActive(false);

        File.AppendAllText(dataFilePath, "experimentData\n");
        yield return StartCoroutine(GameLogic());
    }

    /// <summary>
    /// 实验前练习
    /// </summary>
    /// <returns></returns>
    IEnumerator TryTrial()
    {
        while (countGame <= 15)
        {
            var currentCombination = ExperimentCombinations[currentCombinationIndex];
            xSpeed = currentCombination.speed;
            currentSpeedVector = currentCombination.SpeedVector;
            Vector3 xVector = new Vector3(1, 0, 0);
            Vector3 yVector = new Vector3(0, 1, 0);
            Vector3 zVector = new Vector3(0, 0, 1);
            currentXAngle = Mathf.Acos(Vector3.Dot(currentSpeedVector, xVector)) * Mathf.Rad2Deg;
            currentYAngle = Mathf.Acos(Vector3.Dot(currentSpeedVector, yVector)) * Mathf.Rad2Deg;
            currentZAngle = Mathf.Acos(Vector3.Dot(currentSpeedVector, zVector)) * Mathf.Rad2Deg;

            //小球位置重置
            transform.position = startPos.position;
            ballRenderer.material = visualMat;
            isBallMoving = true;
            isTriggerEnd = false;
            isDelayingStop = false;

            StartCoroutine(BallMove());

            // 暂停协程的执行，点击空格，触碰到终点墙，小球不能移动出现其一后，再执行之后代码
            yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space) || !isBallMoving || isTriggerEnd);

            if (Input.GetKeyDown(KeyCode.Space) && isDelayingStop)
            {
                isTriggerEnd = true;
                isBallMoving = false;
                StopCoroutine(DelayStop());
            }

            // 停止小球移动
            isBallMoving = false;
            ballRenderer.material = visualMat;

            // 显示结果
            resultBrand.SetActive(true);
            float distanceToWall = Mathf.Round(endPos.transform.position.x - transform.position.x - 3f);

            if (isTriggerEnd)
            {
                result.text = "已经触碰到墙了，距离终点：" + distanceToWall;
            }
            else
            {
                result.text = $"距离终点：{distanceToWall}";
            }

            SaveDataToFile(countGame, currentXAngle, currentYAngle, currentZAngle, xSpeed, distanceToWall);

            // 实验延迟3秒后继续
            yield return new WaitForSecondsRealtime(resultDisplayTime);
            resultBrand.SetActive(false);

            countGame++;
            currentCombinationIndex++;
        }
    }

    /// <summary>
    /// 游戏主逻辑
    /// </summary>
    IEnumerator GameLogic()
    {
        while (countGame <= 150)
        {
            var currentCombination = ExperimentCombinations[currentCombinationIndex];
            xSpeed = currentCombination.speed;
            currentSpeedVector = currentCombination.SpeedVector;
            Vector3 xVector = new Vector3(1, 0, 0);
            Vector3 yVector = new Vector3(0, 1, 0);
            Vector3 zVector = new Vector3(0, 0, 1);
            currentXAngle = Mathf.Acos(Vector3.Dot(currentSpeedVector, xVector)) * Mathf.Rad2Deg;
            currentYAngle = Mathf.Acos(Vector3.Dot(currentSpeedVector, yVector)) * Mathf.Rad2Deg;
            currentZAngle = Mathf.Acos(Vector3.Dot(currentSpeedVector, zVector)) * Mathf.Rad2Deg;

            //小球位置重置
            transform.position = startPos.position;
            ballRenderer.material = visualMat;
            isBallMoving = true;
            isTriggerEnd = false;
            isDelayingStop = false;

            StartCoroutine(BallMove());

            // 暂停协程的执行，点击空格，触碰到终点墙，小球不能移动出现其一后，再执行之后代码
            yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space) || !isBallMoving || isTriggerEnd);

            if (Input.GetKeyDown(KeyCode.Space) && isDelayingStop)
            {
                isTriggerEnd = true;
                isBallMoving = false;
                StopCoroutine(DelayStop());
            }

            // 停止小球移动
            isBallMoving = false;
            ballRenderer.material = visualMat;

            // 显示结果
            resultBrand.SetActive(true);
            float distanceToWall = Mathf.Round(endPos.transform.position.x - transform.position.x - 3f);

            if (isTriggerEnd)
            {
                result.text = "已经触碰到墙了，距离终点：" + distanceToWall;
            }
            else
            {
                result.text = $"距离终点：{distanceToWall}";
            }

            SaveDataToFile(countGame, currentXAngle, currentYAngle, currentZAngle, xSpeed, distanceToWall);

            // 实验延迟3秒后继续
            yield return new WaitForSecondsRealtime(resultDisplayTime);

            // 每54次实验后显示休息按钮
            if (countGame % 30 == 0)
            {
                StartCoroutine(CountdownTime());
                yield return new WaitUntil(() => !waitingForRest);  //点击按钮开始
                startBtnBrand.SetActive(false);
            }

            resultBrand.SetActive(false);

            countGame++;

            currentRepetition[currentCombinationIndex]--;

            do
            {
                currentCombinationIndex = Random.Range(0, 15);
            }
            while (currentRepetition[currentCombinationIndex] == 0);

            if (currentRepetition.All(rep => rep == 0))
            {
                break;
            }
        }
        //完成150次试验后返回首页
        SceneManager.LoadScene("Index");
    }

    /// <summary>
    /// 写入每次实验结果的函数
    /// </summary>
    private void SaveDataToFile(int trial, float xAngle, float yAngle, float zAngle, float xSpeed, float distance)
    {
        try
        {
            using (StreamWriter writer = new StreamWriter(dataFilePath, true))
            {
                writer.WriteLine($"{trial}, {xAngle}, {yAngle}, {zAngle}, {xSpeed}, {distance}");
            }
        }
        catch (IOException e)
        {
            Debug.LogError($"写入文件失败: {e.Message}");
        }
    }

    /// <summary>
    /// 小球移动
    /// </summary>
    IEnumerator BallMove()
    {
        isBallMoving = true;
        ballRenderer.material = visualMat;
        float visualEndTime;
        if (timeOrDistance)
        {
            visualEndTime = Time.time + invisualForwardTime;
        }
        else
        {
            visualEndTime = invisualForwardDistance / xSpeed + Time.time;
        }
        // 计算实际速度（确保X轴分速度 = xSpeed）
        float speed = xSpeed / currentSpeedVector.x;
        Vector3 velocity = currentSpeedVector * speed;
        while (isBallMoving)
        {
            transform.Translate(velocity * Time.deltaTime);
            if (visualEndTime <= Time.time)
            {
                ballRenderer.material = invisualMat;
            }
            yield return null;
        }
    }

    /// <summary>
    /// 触碰终点墙
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("EndPos") && !isDelayingStop && !isTriggerEnd)
        {
            StartCoroutine(DelayStop());
        }
    }

    /// <summary>
    /// 触碰终点墙后延迟效果
    /// </summary>
    IEnumerator DelayStop()
    {
        isDelayingStop = true;
        float timer = 0f;

        while (timer < delayBeforeStop && isBallMoving)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (isBallMoving)
        {
            isTriggerEnd = true;
            isBallMoving = false;
        }

        isDelayingStop = false;
    }

    /// <summary>
    /// 休息按钮点击事件
    /// </summary>
    private void OnStartButtonClick()
    {
        waitingForRest = false;
    }

    /// <summary>
    /// 倒计时
    /// </summary>
    /// <returns></returns>
    IEnumerator CountdownTime()
    {
        float currentTime = restTime;
        waitingForRest = true;
        startBtnBrand.SetActive(true);
        while (currentTime >= 0 && waitingForRest)
        {
            yield return new WaitForSeconds(1f);
            currentTime--;
            restContent.text = $"实验较长，休息一下吧!\n剩余时间：" + currentTime;
        }
        OnStartButtonClick();
    }

    /// <summary>
    /// 利用洗牌算法，实现每次游戏的27组数据随即排列
    /// </summary>
    private void ShuffleExperimentOrder()
    {
        for (int i = ExperimentCombinations.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            var temp = ExperimentCombinations[i];
            ExperimentCombinations[i] = ExperimentCombinations[randomIndex];
            ExperimentCombinations[randomIndex] = temp;
        }
    }

    /// <summary>
    /// 试验次数的UI
    /// </summary>
    private void OnGUI()
    {
        GUIStyle countText = new GUIStyle(GUI.skin.label);
        countText.fontSize = 24;
        countText.normal.textColor = Color.white;
        GUI.Label(new Rect(Screen.width / 2 - 50, 20, 150, 80), "第" + countGame + "次实验", countText);
    }
}
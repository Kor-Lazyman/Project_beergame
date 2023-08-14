using System;
using System.Collections.Generic;
using System.Linq;
using SimSharp;

namespace config
{
    public class Item
    {
        public int ID { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public int InitLevel { get; set; }
        public int ManuOrderCycle { get; set; }
        public int SupLeadTime { get; set; }
        public int LotSizeOrder { get; set; }
        public int HoldCost { get; set; }
        public int ShortageCost { get; set; }
        public int PurchaseCost { get; set; }
        public int SetupCostRaw { get; set; }
        // ... (다른 속성들도 추가)
    }

    public class Process
    {
        public int ID { get; set; }
        public int ProductionRate { get; set; }
        public List<Item> InputList { get; set; }
        public List<int> InputUseCount { get; set; }
        public Item? Output { get; set; }
        public int ProcessCost { get; set; }
        public int ProStopCost { get; set; }
        // ... (다른 속성들도 추가)
    }

    public class Variables
    {
        public static bool Ver_simulation = false;
        public static bool Ver_print = false;
        public static bool COST_VALID = false;
        public static bool VISUAL = false;
        public static bool SPECIFIC_HOLDING_COST = false;
        public static List<Process> EventHoldingCost = new List<Process>();

        public static int SIM_TIME = 20;
        public static int INITIAL_INVENTORY = 100;
        public static int EPISODES = 200;
        public static List<double> total_cost_per_day = new List<double>();
        public static int batch_size = 32;
        public static List<int[]> action_space = new List<int[]>();
        public static int[] values = { 0, 10, 20 };

        public static double discount_factor = 0.98;
        public static double epsilon_greedy = 1.0;
        public static double epsilon_min = 0.01;
        public static double epsilon_decay = 0.99995;
        public static double learning_rate = 0.001;
        public static int max_memory_size = 2000;
        public static int target_update_frequency = 1;

        public static Dictionary<int, Item> I = new Dictionary<int, Item>();
        public static Dictionary<int, Process> P = new Dictionary<int, Process>();

        static Variables()
        {
            // I 딕셔너리 초기화
            I[0] = new Item { ID = 0, Type = "Product", Name = "PRODUCT", InitLevel = 30, ManuOrderCycle = 7, HoldCost = 5, ShortageCost = 10, SetupCostRaw = 50 };
            I[1] = new Item { ID = 1, Type = "Raw Material", Name = "RAW MATERIAL 1.1", InitLevel = 30, ManuOrderCycle = 7, SupLeadTime = 7, HoldCost = 1, ShortageCost = 2, SetupCostRaw = 20 };
            I[2] = new Item { ID = 2, Type = "Raw Material", Name = "RAW MATERIAL 2.1", InitLevel = 30, ManuOrderCycle = 7, SupLeadTime = 7, HoldCost = 1, ShortageCost = 2, SetupCostRaw = 20 };
            I[3] = new Item { ID = 3, Type = "Raw Material", Name = "RAW MATERIAL 2.2", InitLevel = 30, ManuOrderCycle = 7, SupLeadTime = 7, HoldCost = 1, ShortageCost = 2, SetupCostRaw = 20 };
            I[4] = new Item { ID = 4, Type = "WIP", Name = "WIP 1", InitLevel = 30, HoldCost = 2, ShortageCost = 2 };

            // P 딕셔너리 초기화
            P[0] = new Process { ID = 0, ProductionRate = 3, InputList = new List<Item> { I[1] }, InputUseCount = new List<int> { 1 }, Output = I[4], ProcessCost = 5, ProStopCost = 2 };
            P[1] = new Process { ID = 1, ProductionRate = 2, InputList = new List<Item> { I[2], I[3], I[4] }, InputUseCount = new List<int> { 1, 1, 1 }, Output = I[0], ProcessCost = 6, ProStopCost = 3 };
        }

        // 시뮬레이션 코드를 작성해야 합니다.
        // ...
    }
}

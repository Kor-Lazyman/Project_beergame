using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic;
using System.Xml.Linq;
using SimSharp;

namespace config
{
    public class Item
    {
        public int ID { get; set; }
        public string TYPE { get; set; }
        public string NAME { get; set; }
        public int INIT_LEVEL { get; set; }
        public int MANU_ORDER_CYCLE { get; set; }
        public int SUP_LEAD_TIME { get; set; }
        public int LOT_SIZE_ORDER { get; set; }
        public int HOLD_COST { get; set; }
        public int SHORTAGE_COST { get; set; }
        public int PURCHASE_COST { get; set; }
        public int SETUP_COST_RAW { get; set; }

        public int CUST_ORDER_CYCLE { get; set; }
        public int DEMAND_QUANTITY { get; set; }
        public int SETUP_COST_PRO { get; set; }
        public int DELIVERY_COST { get; set; }
        public int DUE_DATE { get; set; }
        public int BACKORDER_COST { get; set; }

        // ... (다른 속성들도 추가)
    }

    public class Process
    {
        public int ID { get; set; }
        public int PRODUCTION_RATE { get; set; }
        public List<Item> INPUT_LIST { get; set; }
        public List<int> INPUT_USE_COUNT { get; set; }
        public Item? OUTPUT { get; set; }
        public int PROCESS_COST { get; set; }
        public int PRO_STOP_COST { get; set; }
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
            I[0] = new Item { ID= 0, TYPE= "Product", NAME="PRODUCT", INIT_LEVEL=30, CUST_ORDER_CYCLE= 7, DEMAND_QUANTITY= 21, HOLD_COST= 5, SHORTAGE_COST=10, SETUP_COST_PRO= 50, DELIVERY_COST=10, DUE_DATE= 2, BACKORDER_COST=5 };
            I[1] = new Item { ID= 1, TYPE="Raw Material", NAME= "RAW MATERIAL 1.1", INIT_LEVEL= 30, MANU_ORDER_CYCLE= 7, SUP_LEAD_TIME= 7, LOT_SIZE_ORDER= 21, HOLD_COST= 1, SHORTAGE_COST= 2, PURCHASE_COST= 3, SETUP_COST_RAW= 20 };
            I[2] = new Item { ID= 2, TYPE="Raw Material", NAME="RAW MATERIAL 2.1", INIT_LEVEL=30, MANU_ORDER_CYCLE=7, SUP_LEAD_TIME=7, LOT_SIZE_ORDER= 21, HOLD_COST=1, SHORTAGE_COST= 2, PURCHASE_COST= 3, SETUP_COST_RAW=20 };
            I[3] = new Item { ID=3, TYPE= "Raw Material",NAME= "RAW MATERIAL 2.2", INIT_LEVEL=30, MANU_ORDER_CYCLE= 7, SUP_LEAD_TIME= 7, LOT_SIZE_ORDER=21, HOLD_COST=1, SHORTAGE_COST=2, PURCHASE_COST=3, SETUP_COST_RAW=20 };
            I[4] = new Item { ID= 4, TYPE= "WIP", NAME= "WIP 1", INIT_LEVEL= 30, HOLD_COST= 2, SHORTAGE_COST=2 };

            // P 딕셔너리 초기화
            P[0] = new Process { ID = 0, PRODUCTION_RATE = 3, INPUT_LIST = new List<Item> { I[1] }, INPUT_USE_COUNT = new List<int> { 1 }, OUTPUT = I[4], PROCESS_COST = 5, PRO_STOP_COST = 2 };
            P[1] = new Process { ID = 1, PRODUCTION_RATE = 2, INPUT_LIST = new List<Item> { I[2], I[3], I[4] }, INPUT_USE_COUNT = new List<int> { 1, 1, 1 }, OUTPUT = I[0], PROCESS_COST = 6, PRO_STOP_COST = 3 };
            foreach (int i in values)
            {
                foreach (int j in values)
                {
                    foreach (int k in values)
                    {
                        action_space.Add(new int[] { i, j, k });
                    }
                }
            }
        }


        // 시뮬레이션 코드를 작성해야 합니다.
        // ...
    }
}

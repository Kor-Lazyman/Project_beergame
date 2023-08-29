using config;
using SimSharp;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Envinorment
{
    class dictionary
    {
        public static Dictionary<int, Item> I = Variables.I;
        public static void Make_Init()
        {
            I = Variables.I;
        }
    }

    class Inventory
    {
        public int item_id { get; set; }
        public int Level { get; set; }
        public int Holding_cost { get; set; }
        public int Shortage_cost { get; set; }
        public List<int> Level_over_time { get; set; }
        public List<int> Inveventory_cost_over_time { get; set; }
        public List<int> Total_inven_cost { get; set; }

        public Inventory(int item_id, int holding_cost, int shortage_cost, int initial_level)
        {
            this.item_id = item_id;
            Level = initial_level;
            this.Holding_cost = holding_cost;
            this.Shortage_cost = shortage_cost;
            Level_over_time = new List<int>();
            Inveventory_cost_over_time = new List<int>();
            Total_inven_cost = new List<int>();
        }
    }
    class Provider
    {
        public Simulation Env { get; set; }
        public string Name { get; set; }
        public int Item_id { get; set; }

        public Provider(Simulation env, string name, int item_id)
        {
            this.Env = env;
            Name = name;
            Item_id = item_id;
        }
        public IEnumerable<Event> Deliver(int order_size, Inventory inventory)
        {
            // 리드 타임
            yield return Env.Timeout(TimeSpan.FromHours(dictionary.I[Item_id].SUP_LEAD_TIME * 24));
            inventory.Level += order_size;
            if (config.Variables.Ver_simulation)
            {
                //Console.WriteLine($"{(Env.Now - new DateTime(1970, 1, 1, 0, 0, 0)).TotalDays}: {Name}has delivered {dictionary.I[Item_id].NAME}units of{order_size}.");
            }
        }
    }
    class Procurement
    {
        public Simulation Env { get; set; }
        public int Item_id { get; set; }
        public int Purchase_cost { get; set; }
        public int Setup_cost { get; set; }
        public List<int> Purchase_cost_over_time { get; set; }
        public List<int> Setup_cost_over_time { get; set; }
        public int Daily_procurement_cost { get; set; }

        public Procurement(Simulation env, int item_id, int purchase_cost, int setup_cost)
        {
            Env = env;
            Item_id = item_id;
            Purchase_cost = purchase_cost;
            Setup_cost = setup_cost;
            Purchase_cost_over_time = new List<int>();
            Setup_cost_over_time = new List<int>();
            Daily_procurement_cost = 0;
        }

        public IEnumerable<Event> Order(Provider provider, Inventory inventory)
        {
            while (true)
            {
                // 공급업체에 주문 생성
                yield return Env.Timeout(TimeSpan.FromHours(dictionary.I[Item_id].MANU_ORDER_CYCLE * 24));
                // 이 부분은 에이전트의 액션으로 변경될 것입니다.
                int order_size = dictionary.I[Item_id].LOT_SIZE_ORDER;
                if (Variables.Ver_simulation)
                {
                    //Console.WriteLine($"{(Env.Now - new DateTime(1970, 1, 1, 0, 0, 0)).TotalDays}: Placed an order for{dictionary.I[Item_id].NAME} units of{order_size}");
                }
                Env.Process(provider.Deliver(order_size, inventory));
            }
        }
    }
    class Production
    {
        public Simulation Env { get; set; }
        public string Name { get; set; }
        public int Process_id { get; set; }
        public int Production_rate { get; set; }
        public Item Output { get; set; }
        public List<Inventory> Input_inventories { get; set; }
        public Inventory Output_inventory { get; set; }
        public int Processing_cost { get; set; }
        public List<int> Processing_cost_over_time { get; set; }
        public int Daily_production_cost { get; set; }
        public int Process_stop_cost { get; set; }

        public Production(Simulation env, string name, int process_id, int production_rate, Item output, List<Inventory> input_inventories, Inventory output_inventory, int processing_cost)
        {
            Env = env;
            Name = name;
            Process_id = process_id;
            Production_rate = production_rate;
            Output = output;
            Input_inventories = input_inventories;
            Output_inventory = output_inventory;
            Processing_cost = processing_cost;
            Processing_cost_over_time = new List<int>();
            Daily_production_cost = 0;
            Process_stop_cost = 0;


        }

        public IEnumerable<Event> Process()
        {
            DateTime startday = new DateTime(1970, 1, 1, 0, 0, 0);
            while (true)
            {

                TimeSpan elapsedTime = Env.Now - startday;
                int day = (int)elapsedTime.TotalDays + 1;
                int hours = (int)elapsedTime.TotalHours;
                bool shortageCheck = false;

                foreach (var inven in Input_inventories)
                {
                    int useCount = Variables.P[Process_id].INPUT_USE_COUNT[Input_inventories.IndexOf(inven)];
                    if (inven.Level < useCount)
                    {
                        shortageCheck = true;
                    }
                }

            if (shortageCheck)
                {
                    Process_stop_cost += Variables.P[Process_id].PRO_STOP_COST;
                    if (Variables.Ver_simulation)
                    {
                        //Console.WriteLine($"Day:{day} Hours:{hours}: Stop {Name} due to a shortage of input materials or WIPs.");
                        //Console.WriteLine($"Day:{day} Hours:{hours}: Process stop cost :: {Process_stop_cost}");
                    }
                    yield return Env.Timeout(TimeSpan.FromHours(24));
                }
                else
                {
                    int total_use_count = Variables.P[Process_id].INPUT_USE_COUNT.Sum();

                    double processing_time = 24.0 / Production_rate;
                    if (Variables.Ver_simulation)
                    {
                        //Console.WriteLine($"Day: {day} Hours:{hours}: Process {Process_id} begins ");

                    }

                    for (int i = 0; i < Input_inventories.Count; i++)
                    {
                        Inventory inven = Input_inventories[i];
                        int use_count = Variables.P[Process_id].INPUT_USE_COUNT[i];
                        inven.Level -= use_count;
                        if (Variables.Ver_simulation)
                        {
                            //Console.WriteLine($"Day: {day} Hours:{hours}:  Inventory level of {dictionary.I[inven.item_id].NAME}: {inven.Level}");
                            double holdingCost = inven.Level * dictionary.I[inven.item_id].HOLD_COST;
                            //Console.WriteLine($"Day: {day} Hours:{hours}:Holding cost of {dictionary.I[inven.item_id].NAME}: {Math.Round(holdingCost, 2)}");

                        }

                    }

                    Output_inventory.Level += 1;

                    if (Variables.Ver_simulation)
                    {
                        //Console.WriteLine($"Day: {day} Hours:{hours}:  A unit of {dictionary.I[Output_inventory.item_id].TYPE} has been produced");
                        //Console.WriteLine($"Day: {day} Hours:{hours}:  Inventory level of {dictionary.I[Output_inventory.item_id].TYPE}: {Output_inventory.Level}");
                        double outputHoldingCost = Output_inventory.Level * dictionary.I[Output_inventory.item_id].HOLD_COST;
                        //Console.WriteLine($"Day: {day} Hours:{hours}: Holding cost of {dictionary.I[Output_inventory.item_id].TYPE}: {Math.Round(outputHoldingCost, 2)}");
                    }
                    yield return Env.Timeout(TimeSpan.FromHours(processing_time));
                }

            }
        }
    }
    class Sales
    {
        public Simulation Envinorment { get; set; }
        public int ItemId { get; set; }
        public int DeliveryCost { get; set; }
        public int SetupCost { get; set; }
        public List<int> SellingCostOverTime { get; set; }
        public int DailySellingCost { get; set; }
        public int LossCost { get; set; }

        public Sales(Simulation env, int item_id, int delivery_cost, int setup_cost)
        {
            Envinorment = env;
            ItemId = item_id;
            DeliveryCost = delivery_cost;
            SetupCost = setup_cost;
            SellingCostOverTime = new List<int>();
            DailySellingCost = 0;
            LossCost = 0;
        }

        public IEnumerable<Event> Delivery(int item_id, int order_size, Inventory product_inventory)
        {
            yield return Envinorment.Timeout(TimeSpan.FromHours(dictionary.I[item_id].DUE_DATE * 24));

            if (product_inventory.Level < order_size)
            {
                int numShortages = Math.Abs(product_inventory.Level - order_size);
                if (product_inventory.Level > 0)
                {
                    if (Variables.Ver_simulation)
                    {
                        //Console.WriteLine($"{(Envinorment.Now - new DateTime(1970, 1, 1, 0, 0, 0)).TotalDays}: {product_inventory.Level} units of the product have been delivered to the customer");
                    }

                    product_inventory.Level -= order_size;
                }

                LossCost = dictionary.I[item_id].BACKORDER_COST * numShortages;

                if (Variables.Ver_simulation)
                {
                    //Console.WriteLine($"[Cost of Loss] {LossCost}");
                    //Console.WriteLine($"Unable to deliver {Envinorment.Now - new DateTime(1970, 1, 1, 0, 0, 0)}: {numShortages}units to the customer due to product shortage ");
                }
            }
            else
            {
                product_inventory.Level -= order_size;
                if (Variables.Ver_simulation)
                {
                    //Console.WriteLine($"{(Envinorment.Now - new DateTime(1970, 1, 1, 0, 0, 0)).TotalDays}: {order_size} units of the product have been delivered to the customer");
                }

            }
        }
    }
    class Customer
    {
        public Simulation Envinorment { get; set; }
        public string Name { get; set; }
        public int ItemId { get; set; }
        public List<int> OrderHistory { get; set; }

        public Customer(Simulation env, string name, int item_id)
        {
            Envinorment = env;
            Name = name;
            ItemId = item_id;
            OrderHistory = new List<int>();
        }

        public IEnumerable<Event> Order(Sales sales, Inventory product_inventory)
        {
            while (true)
            {
                yield return Envinorment.Timeout(TimeSpan.FromHours(dictionary.I[ItemId].CUST_ORDER_CYCLE * 24));
                int order_size = dictionary.I[ItemId].DEMAND_QUANTITY;
                OrderHistory.Add(order_size);

                if (Variables.Ver_simulation)
                {
                    //Console.WriteLine($"{(Envinorment.Now - new DateTime(1970, 1, 1, 0, 0, 0)).TotalDays}: The customer has placed an order for {dictionary.I[ItemId].NAME}  units of {order_size}");
                }

                Envinorment.Process(sales.Delivery(ItemId, order_size, product_inventory));
            }
        }
    }
    class Program
    {
        //DataBase생성 
        public static double total_cost = 0;
        public static int[] indexes =new int[Variables.SIM_TIME];
        public static void Main(string[] args)
        {

            double[,,,] policy = new double[Variables.SIM_TIME,1,1,3];
            double gamma = 0.01;
            double value = 0;
          // 시뮬레이션 루프

            // 값을 할당하여 배열 채우기
            for (int Episodes = 0; Episodes < 10000; Episodes++)
            {
                int max_index=0;
        // Create_env 메서드를 호출하여 시뮬레이션 구성 요소 초기화
        Tuple<SimSharp.Simulation, List<Inventory>, List<Procurement>, List<Production>, Sales, Customer, List<Provider>> simulationData = Create_env();


                // 시뮬레이션 데이터 추출
                Simulation simsharp_env = simulationData.Item1;
                List<Inventory> inventoryList = simulationData.Item2;
                List<Procurement> procurementList = simulationData.Item3;
                List<Production> productionList = simulationData.Item4;

                Sales sales = simulationData.Item5;

                int cost = 0;
                dictionary.Make_Init();
                List<List<int>> episode_data = new List<List<int>>();

                List<List<int>> tmp2 = new List<List<int>>();
                if (Episodes % 1000 == 0)
                {
                    Console.WriteLine(Episodes);
                    Thread.Sleep(1000);
                }
                int basic_action = Variables.I[1].LOT_SIZE_ORDER;
                for (int day = 0; day < Variables.SIM_TIME; day++)
                {
                    
                    int previous_Product = inventoryList[0].Level;
                    int previous_Raw = inventoryList[1].Level;
                    for (int action = 0; action < 3; action++)
                    {
                        //Console.WriteLine($"\n===================Day {day + 1} Start===================\n");
                        Simulation test_env = simsharp_env;
                        Dictionary<int, Item> test_I = dictionary.I;
                        dictionary.I[1].LOT_SIZE_ORDER = action;
                        test_env.Run(TimeSpan.FromDays(1));
                        value+=policy[day, previous_Product, previous_Raw, action]*(-(previous_Product+ previous_Raw)) + Math.Pow(gamma, day) * (inventoryList[0].Level + inventoryList[1].Level);
                        inventoryList[0].Level = previous_Product;
                        inventoryList[1].Level = previous_Raw;
                        dictionary.I = test_I;
                    }

                    for(int action = 0;action < 3; action++)
                    {
                        Simulation test_env = simsharp_env;
                        Dictionary<int, Item> test_I = dictionary.I;
                        dictionary.I[1].LOT_SIZE_ORDER = action;
                        test_env.Run(TimeSpan.FromDays(1));
                        double reward = -(previous_Product+ previous_Raw);
                        double next_value = inventoryList[0].Level + inventoryList[1].Level;
                        double temp = reward + Math.Pow(gamma, day) * next_value;
                        inventoryList[0].Level = previous_Product;
                        inventoryList[1].Level = previous_Raw;
                        dictionary.I = test_I;
                        if (temp == value)
                        {
                            max_index=action;
                            indexes[day]=max_index;
                        }
                        else if(temp>value)
                        {
                            value= temp;
                            max_index=action;
                            indexes[day] = max_index;
                        }
                    }
                    policy[day, previous_Product, previous_Raw, max_index] += value;

                }
            }
            for(int day = 0; day < Variables.SIM_TIME; day++)
            {
                Console.WriteLine(indexes[day]);
            }
            
           
        }
            static Tuple<SimSharp.Simulation, List<Inventory>, List<Procurement>, List<Production>, Sales, Customer, List<Provider>> Create_env()
            {

                var SimShrp_env = new SimSharp.Simulation();
                List<Inventory> inventoryList = new List<Inventory>();
                foreach (int i in dictionary.I.Keys)
                {
                    inventoryList.Add(new Inventory(i, dictionary.I[i].HOLD_COST, dictionary.I[i].SHORTAGE_COST, dictionary.I[i].INIT_LEVEL));
                }

                Customer customer = new Customer(SimShrp_env, "CUSTOMER", dictionary.I[0].ID);

                List<Provider> providerList = new List<Provider>();
                List<Procurement> procurementList = new List<Procurement>();
                foreach (int i in dictionary.I.Keys)
                {
                    if (dictionary.I[i].TYPE == "Raw Material")
                    {
                        providerList.Add(new Provider(SimShrp_env, "PROVIDER_" + i.ToString(), i));
                        procurementList.Add(new Procurement(SimShrp_env, dictionary.I[i].ID, dictionary.I[i].PURCHASE_COST, dictionary.I[i].SETUP_COST_RAW));
                    }
                }

                Sales sales = new Sales(SimShrp_env, customer.ItemId, dictionary.I[0].DELIVERY_COST, dictionary.I[0].SETUP_COST_RAW);

                List<Production> productionList = new List<Production>();
                foreach (int i in Variables.P.Keys)
                {
                    Inventory outputInventory = inventoryList[Variables.P[i].OUTPUT.ID];
                    List<Inventory> inputInventories = new List<Inventory>();
                    foreach (var j in Variables.P[i].INPUT_LIST)
                    {

                        inputInventories.Add(inventoryList[j.ID]);
                    }
                    Item Result_item_id = Variables.P[i].OUTPUT;
                    productionList.Add(new Production(SimShrp_env, "PROCESS_" + i.ToString(), Variables.P[i].ID,
                                                       Variables.P[i].PRODUCTION_RATE, Result_item_id, inputInventories, outputInventory, Variables.P[i].PROCESS_COST));
                }

                SimShrp_env.Process(customer.Order(sales, inventoryList[dictionary.I[0].ID]));
                foreach (var production in productionList)
                {
                    SimShrp_env.Process(production.Process());
                }
                for (int i = 0; i < providerList.Count; i++)
                {
                    SimShrp_env.Process(procurementList[i].Order(providerList[i], inventoryList[providerList[i].Item_id]));
                }

                return new Tuple<SimSharp.Simulation, List<Inventory>, List<Procurement>, List<Production>, Sales, Customer, List<Provider>>(
                    SimShrp_env, inventoryList, procurementList, productionList, sales, customer, providerList);
            }
        
    }
}

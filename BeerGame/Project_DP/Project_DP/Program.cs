using config;
using SimSharp;
using Newtonsoft.Json;
using static System.Net.Mime.MediaTypeNames;
using System.Xml;
using System.Text;

namespace envinorment
{



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

        public void Cal_inventory_cost()
        {
            if (Level > 0)
            {
                Inveventory_cost_over_time.Add(Holding_cost * Level);
            }
            else if (Level < 0)
            {
                Inveventory_cost_over_time.Add(Shortage_cost * Math.Abs(Level));
            }
            else
            {
                Inveventory_cost_over_time.Add(0);
            }

            if (Variables.Ver_simulation)
            {

                Console.WriteLine($"[Inventory Cost of {Variables.I[item_id].NAME}]  {Inveventory_cost_over_time[^1]}");

            }
        }

        public void cal_event_holding_cost()
        {
            for (int j = 0; j < Variables.I.Count; j++)
            {
                double daily_holding_cost = 0.0;
                for (int i = 0; i < Variables.SIM_TIME - 1; i++)
                {
                    daily_holding_cost = +daily_holding_cost;
                }
                Console.WriteLine($"[Daily holding Cost of {Variables.I[j].NAME}] {daily_holding_cost}");
            }
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
            yield return Env.Timeout(TimeSpan.FromHours(Variables.I[Item_id].SUP_LEAD_TIME * 24));
            inventory.Level += order_size;
            if (Variables.Ver_simulation)
            {
                Console.WriteLine($"{(Env.Now - new DateTime(1970, 1, 1, 0, 0, 0)).TotalDays}: {Name}has delivered {Variables.I[Item_id].NAME}units of{order_size}.");
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
                yield return Env.Timeout(TimeSpan.FromHours(Variables.I[Item_id].MANU_ORDER_CYCLE * 24));
                // 이 부분은 에이전트의 액션으로 변경될 것입니다.
                int order_size = Variables.I[Item_id].LOT_SIZE_ORDER;
                if (Variables.Ver_simulation)
                {
                    Console.WriteLine($"{(Env.Now - new DateTime(1970, 1, 1, 0, 0, 0)).TotalDays}: Placed an order for{Variables.I[Item_id].NAME} units of{order_size}");
                }
                Env.Process(provider.Deliver(order_size, inventory));
                Cal_procurement_cost();
            }
        }

        public void Cal_procurement_cost()
        {
            Daily_procurement_cost += Purchase_cost * Variables.I[Item_id].LOT_SIZE_ORDER + Setup_cost;
        }

        public void Cal_daily_procurement_cost()
        {
            if (Variables.Ver_simulation)
            {
                Console.WriteLine($"[Daily procurement cost of {Variables.I[Item_id].NAME}]  {Daily_procurement_cost}");
            }
            Daily_procurement_cost = 0;
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
                int day = (int)elapsedTime.TotalDays+1;
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
                        Console.WriteLine($"Day:{day} Hours:{hours}: Stop {Name} due to a shortage of input materials or WIPs.");
                        Console.WriteLine($"Day:{day} Hours:{hours}: Process stop cost :: {Process_stop_cost}");
                    }
                    yield return Env.Timeout(TimeSpan.FromHours(24));
                }
                else//재고가 부족하지 않은 이상 무조건 생산
                {
                    int total_use_count = Variables.P[Process_id].INPUT_USE_COUNT.Sum();

                    double processing_time = 24.0 / Production_rate;
                    if (Variables.Ver_simulation)
                    {
                        Console.WriteLine($"Day: {day} Hours:{hours}: Process {Process_id} begins ");

                    }

                    for (int i = 0; i < Input_inventories.Count; i++)
                    {
                        Inventory inven = Input_inventories[i];
                        int use_count = Variables.P[Process_id].INPUT_USE_COUNT[i];
                        inven.Level -= use_count;
                        if (Variables.Ver_simulation)
                        {
                            Console.WriteLine($"Day: {day} Hours:{hours}:  Inventory level of {Variables.I[inven.item_id].NAME}: {inven.Level}");
                            double holdingCost = inven.Level * Variables.I[inven.item_id].HOLD_COST;
                            Console.WriteLine($"Day: {day} Hours:{hours}:Holding cost of {Variables.I[inven.item_id].NAME}: {Math.Round(holdingCost, 2)}");

                        }

                        etc.EventHoldingCost[day-1][inven.item_id] = ((int)(inven.Level * Variables.I[inven.item_id].HOLD_COST));

                    }

                    Output_inventory.Level += 1;
                    Cal_processing_cost(processing_time);

                    if (Variables.Ver_simulation)
                    {
                        Console.WriteLine($"Day: {day} Hours:{hours}:  A unit of {Variables.I[Output_inventory.item_id].TYPE} has been produced");
                        Console.WriteLine($"Day: {day} Hours:{hours}:  Inventory level of {Variables.I[Output_inventory.item_id].TYPE}: {Output_inventory.Level}");
                        double outputHoldingCost = Output_inventory.Level * Variables.I[Output_inventory.item_id].HOLD_COST;
                        Console.WriteLine($"Day: {day} Hours:{hours}: Holding cost of {Variables.I[Output_inventory.item_id].TYPE}: {Math.Round(outputHoldingCost, 2)}");
                    }
                    etc.EventHoldingCost[day-1][0] = (Output_inventory.Level * Variables.I[Output_inventory.item_id].HOLD_COST);

                    etc.EventHoldingCost[day-1][Variables.I.Count - 1] = (Output_inventory.Level * Variables.I[Output_inventory.item_id].HOLD_COST);
                    yield return Env.Timeout(TimeSpan.FromHours(processing_time));
                }
               
            }
        }

        public void Cal_processing_cost(double processingTime)
        {
            Daily_production_cost += (int)(Processing_cost * processingTime);
        }

        public void Cal_daily_production_cost()
        {
            if (Variables.Ver_simulation)
            {
                Console.WriteLine($"[Daily production cost of {Name}]  {Daily_production_cost}");
            }
            Daily_production_cost = 0;
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
            yield return Envinorment.Timeout(TimeSpan.FromHours(Variables.I[item_id].DUE_DATE * 24));

            if (product_inventory.Level < order_size)
            {
                int numShortages = Math.Abs(product_inventory.Level - order_size);
                if (product_inventory.Level > 0)
                {
                    if (Variables.Ver_simulation)
                    {
                        Console.WriteLine($"{(Envinorment.Now - new DateTime(1970, 1, 1, 0, 0, 0)).TotalDays}: {product_inventory.Level} units of the product have been delivered to the customer");
                    }

                    product_inventory.Level -= order_size;
                    Cal_selling_cost();
                }

                LossCost = Variables.I[item_id].BACKORDER_COST * numShortages;

                if (Variables.Ver_simulation)
                {
                    Console.WriteLine($"[Cost of Loss] {LossCost}");
                    Console.WriteLine($"Unable to deliver {Envinorment.Now - new DateTime(1970, 1, 1, 0, 0, 0)}: {numShortages}units to the customer due to product shortage ");
                }
            }
            else
            {
                product_inventory.Level -= order_size;
                if (Variables.Ver_simulation)
                {
                    Console.WriteLine($"{(Envinorment.Now - new DateTime(1970, 1, 1, 0, 0, 0)).TotalDays}: {order_size} units of the product have been delivered to the customer");
                }

                Cal_selling_cost();
            }
        }

        public void Cal_selling_cost()
        {
            DailySellingCost += DeliveryCost * Variables.I[ItemId].DEMAND_QUANTITY + SetupCost;
        }

        public void Cal_daily_selling_cost()
        {
            if (Variables.Ver_simulation)
            {
                Console.WriteLine($"[Daily selling cost of {Variables.I[ItemId].NAME}]  {DailySellingCost}");
            }

            DailySellingCost = 0;
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
                yield return Envinorment.Timeout(TimeSpan.FromHours(Variables.I[ItemId].CUST_ORDER_CYCLE * 24));
                int order_size = Variables.I[ItemId].DEMAND_QUANTITY;
                OrderHistory.Add(order_size);

                if (Variables.Ver_simulation)
                {
                    Console.WriteLine($"{(Envinorment.Now - new DateTime(1970, 1, 1, 0, 0, 0)).TotalDays}: The customer has placed an order for {Variables.I[ItemId].NAME}  units of {order_size}");
                }

                Envinorment.Process(sales.Delivery(ItemId, order_size, product_inventory));
            }
        }
    }
    class Program
    {
        //DataBase생성 
        public static double total_cost = 0;

        public static void Main(string[] args)
        {

            // Create_env 메서드를 호출하여 시뮬레이션 구성 요소 초기화
            Tuple<SimSharp.Simulation, List<Inventory>, List<Procurement>, List<Production>, Sales, Customer, List<Provider>> simulationData = Create_env();


            // 시뮬레이션 데이터 추출
            Simulation simsharp_env = simulationData.Item1;
            List<Inventory> inventoryList = simulationData.Item2;
            List<Procurement> procurementList = simulationData.Item3;
            List<Production> productionList = simulationData.Item4;
            Sales sales = simulationData.Item5;

            // make_event_holding_cost 메서드 호출로 이벤트 보유 비용 데이터 초기화
            etc.make_event_holding_cost(Variables.SIM_TIME, Variables.I.Count);
            List<List<int>> EventHoldingCost = etc.EventHoldingCost;
           
            List<double> totalCostPerDay = new List<double>();

            // 시뮬레이션 루프


            int[,] Database_Materials = new int[Variables.SIM_TIME, Variables.I.Count];
            double[] Database_Cost = new double[Variables.SIM_TIME];

            // 값을 할당하여 배열 채우기

            for (int day = 0; day < Variables.SIM_TIME; day++)
            {
                Console.WriteLine($"\n===================Day {day + 1} Start===================\n");
               
                // 하루에 대한 비용 계산
                Cal_cost(inventoryList, procurementList, productionList, sales, totalCostPerDay);

                //DataBase 값 입력

                for (int i = 0; i < inventoryList.Count; i++)
                {

                        Database_Materials[day, i] = inventoryList[i].Level;
                }
                Database_Cost[day]=(total_cost);


                simsharp_env.Run(TimeSpan.FromHours(24));


                // 환경의 하루 시뮬레이션 수행
                // 시뮬레이션 시간 전진
            }
     
            etc.SaveToJson("DatabaseMaterials.json", Database_Materials);
            etc.SaveToJson("DatabaseCost.json", Database_Cost);

        }
        static Tuple<SimSharp.Simulation, List<Inventory>, List<Procurement>, List<Production>, Sales, Customer, List<Provider>> Create_env()
        {

            var SimShrp_env = new SimSharp.Simulation();
            List<Inventory> inventoryList = new List<Inventory>();
            foreach (int i in Variables.I.Keys)
            {
                inventoryList.Add(new Inventory(i, Variables.I[i].HOLD_COST, Variables.I[i].SHORTAGE_COST, Variables.I[i].INIT_LEVEL));
            }

            Customer customer = new Customer(SimShrp_env, "CUSTOMER", Variables.I[0].ID);

            List<Provider> providerList = new List<Provider>();
            List<Procurement> procurementList = new List<Procurement>();
            foreach (int i in Variables.I.Keys)
            {
                if (Variables.I[i].TYPE == "Raw Material")
                {
                    providerList.Add(new Provider(SimShrp_env, "PROVIDER_" + i.ToString(), i));
                    procurementList.Add(new Procurement(SimShrp_env, Variables.I[i].ID, Variables.I[i].PURCHASE_COST, Variables.I[i].SETUP_COST_RAW));
                }
            }

            Sales sales = new Sales(SimShrp_env, customer.ItemId, Variables.I[0].DELIVERY_COST, Variables.I[0].SETUP_COST_RAW);

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

            SimShrp_env.Process(customer.Order(sales, inventoryList[Variables.I[0].ID]));
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

        public static void Cal_cost(List<Inventory> inventoryList, List<Procurement> procurementList, List<Production> productionList, Sales sales, List<double> total_cost_per_day)
        {


            total_cost = 0;
            // Calculate the cost models
            foreach (var inven in inventoryList)
            {

                inven.Cal_inventory_cost();

            }
            foreach (var production in productionList)
            {
                production.Cal_daily_production_cost();
            }
            foreach (var procurement in procurementList)
            {
                procurement.Cal_daily_procurement_cost();
            }
            sales.Cal_daily_selling_cost();

            // Calculate the total cost for the current day and append to the list
            
            foreach (var inven in inventoryList)
            {
                total_cost += inven.Inveventory_cost_over_time.Sum();
            }
            foreach (var production in productionList)
            {
                total_cost += production.Daily_production_cost;
            }
            foreach (var procurement in procurementList)
            {
                total_cost += procurement.Daily_procurement_cost;
            }
            total_cost += sales.DailySellingCost;
            total_cost_per_day.Add(total_cost);

            // Reset values for the next day's calculation
            foreach (var inven in inventoryList)
            {
                inven.Inveventory_cost_over_time.Clear();
            }
            foreach (var production in productionList)
            {
                production.Daily_production_cost = 0;
            }
            foreach (var procurement in procurementList)
            {
                procurement.Daily_procurement_cost = 0;
            }
            sales.DailySellingCost = 0;

        }


    }
    class etc
    {
        public static List<List<int>> EventHoldingCost;

        public static void make_event_holding_cost(int time, int itemCount)
        {
            EventHoldingCost = new List<List<int>>();

            for (int i = 0; i < time; i++)
            {
                List<int> num = new List<int>();
                for (int j = 0; j < itemCount; j++)
                {
                    num.Add(999999);
                }
                EventHoldingCost.Add(num);
            }

        }
        public static void SaveToJson<T>(string filename, T data)
        {
            string jsonData = JsonConvert.SerializeObject(data);

            // UTF-8로 인코딩하여 파일에 저장
            File.WriteAllText(filename, jsonData, Encoding.UTF8);

            Console.WriteLine($"Saved {filename} with UTF-8 encoding");
        }
    }

}
 

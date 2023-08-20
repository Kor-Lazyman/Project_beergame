using config;
using SimSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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

        public void cal_inventory_cost()
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
                Console.WriteLine($"{Env.Now}: {Name}이(가) {Variables.I[Item_id].NAME}의 {order_size}개를 납품하였습니다.");
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
                    Console.WriteLine($"{Env.Now}: {Variables.I[Item_id].NAME}의 {order_size}개 주문 생성");
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
                Console.WriteLine($"[{Variables.I[Item_id].NAME}의 일일 조달 비용]  {Daily_procurement_cost}");
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
            int day = 0;
            while (true)
            {
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
                        Console.WriteLine($"{Env.Now}: {Name}이(가) 원자재 또는 WIP 부족으로 작업을 중지합니다.");
                        Console.WriteLine($"{Env.Now}: 작업 중지 비용: {Process_stop_cost}");
                    }
                    yield return Env.Timeout(TimeSpan.FromHours(24));
                }
                else
                {
                    int total_use_count = Variables.P[Process_id].INPUT_USE_COUNT.Sum();

                    double processing_time = 24.0 / Production_rate;
                    yield return Env.Timeout(TimeSpan.FromHours(processing_time));

                    DateTime now = Env.Now;
                    DateTime start = DateTime.MinValue;
                    TimeSpan elapsedTime = now - start;
                   
                    

                    if (Variables.Ver_simulation)
                    {
                        Console.WriteLine($"{Env.Now}: {Process_id} 작업 시작");
                    }

                    for (int i = 0; i < Input_inventories.Count; i++)
                    {
                        Inventory inven = Input_inventories[i];
                        int use_count = Variables.P[Process_id].INPUT_USE_COUNT[i];
                        inven.Level -= use_count;
                        if (Variables.Ver_simulation)
                        {
                            Console.WriteLine($"{Env.Now}: {Variables.I[inven.item_id].NAME}의 재고 수준: {inven.Level}");
                            double holdingCost = inven.Level * Variables.I[inven.item_id].HOLD_COST;
                            Console.WriteLine($"{Env.Now}: {Variables.I[inven.item_id].NAME}의 보유 비용: {Math.Round(holdingCost, 2)}");
                        }

                        etc.EventHoldingCost[day][inven.item_id]=(Math.Round((inven.Level * Variables.I[inven.item_id].HOLD_COST, 2)));
                    }

                    Output_inventory.Level += 1;
                    Cal_processing_cost(processing_time);

                    if (Variables.Ver_simulation)
                    {
                        Console.WriteLine($"{Env.Now}: {Output} 1개를 생산했습니다.");
                        Console.WriteLine($"{Env.Now}: {Output}의 재고 수준: {Output_inventory.Level}");
                        double outputHoldingCost = Output_inventory.Level * Variables.I[Output_inventory.item_id].HOLD_COST;
                        Console.WriteLine($"{Env.Now}: {Output}의 보유 비용: {Math.Round(outputHoldingCost, 2)}");
                    }
                    etc.EventHoldingCost[day][-1]=(Math.Round(Output_inventory.Level * Variables.I[Output_inventory.item_id].HOLD_COST, 2));

                    etc.EventHoldingCost[day][0]=(Math.Round((Output_inventory.Level * Variables.I[Output_inventory.item_id].HOLD_COST, 2)));
                }
                
               
                day = day + 1;
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
                Console.WriteLine($"[{Name}의 일일 생산 비용]  {Daily_production_cost}");
            }
            Daily_production_cost = 0;
        }
    }

    class Sales
    {
        public Simulation Environment { get; set; }
        public int ItemId { get; set; }
        public int DeliveryCost { get; set; }
        public int SetupCost { get; set; }
        public List<int> SellingCostOverTime { get; set; }
        public int DailySellingCost { get; set; }
        public int LossCost { get; set; }

        public Sales(Simulation env, int item_id, int delivery_cost, int setup_cost)
        {
            Environment = env;
            ItemId = item_id;
            DeliveryCost = delivery_cost;
            SetupCost = setup_cost;
            SellingCostOverTime = new List<int>();
            DailySellingCost = 0;
            LossCost = 0;
        }

        public IEnumerable<Event> Delivery(int item_id, int order_size, Inventory product_inventory)
        {
            yield return Environment.Timeout(TimeSpan.FromHours(Variables.I[item_id].DUE_DATE * 24));

            if (product_inventory.Level < order_size)
            {
                int numShortages = Math.Abs(product_inventory.Level - order_size);
                if (product_inventory.Level > 0)
                {
                    if (Variables.Ver_simulation)
                    {
                        Console.WriteLine($"{Environment.Now}: 고객에게 제품 {product_inventory.Level}개가 전달되었습니다.");
                    }

                    product_inventory.Level -= order_size;
                    Cal_selling_cost();
                }

                LossCost = Variables.I[item_id].BACKORDER_COST * numShortages;

                if (Variables.Ver_simulation)
                {
                    Console.WriteLine($"[손실 비용] {LossCost}");
                    Console.WriteLine($"{Environment.Now}: 제품 부족으로 고객에게 {numShortages}개의 제품을 전달하지 못했습니다.");
                }
            }
            else
            {
                product_inventory.Level -= order_size;
                if (Variables.Ver_simulation)
                {
                    Console.WriteLine($"{Environment.Now}: 고객에게 제품 {order_size}개가 전달되었습니다.");
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
                Console.WriteLine($"[{Variables.I[ItemId].NAME}의 일일 판매 비용]  {DailySellingCost}");
            }

            DailySellingCost = 0;
        }
    }

    class Customer
    {
        public Simulation Environment { get; set; }
        public string Name { get; set; }
        public int ItemId { get; set; }
        public List<int> OrderHistory { get; set; }

        public Customer(Simulation env, string name, int item_id)
        {
            Environment = env;
            Name = name;
            ItemId = item_id;
            OrderHistory = new List<int>();
        }

        public IEnumerable<Event> Order(Sales sales, Inventory product_inventory)
        {
            while (true)
            {
                yield return Environment.Timeout(TimeSpan.FromHours(Variables.I[ItemId].CUST_ORDER_CYCLE * 24));
                int order_size = Variables.I[ItemId].DEMAND_QUANTITY;
                OrderHistory.Add(order_size);

                if (Variables.Ver_simulation)
                {
                    Console.WriteLine($"{Environment.Now}: 고객이 {Variables.I[ItemId].NAME}의 제품 {order_size}개를 주문했습니다.");
                }

                Environment.Process(sales.Delivery(ItemId, order_size, product_inventory));
            }
        }
    }
    class Program
    {
        public static void Main(string[] args)
        {

            // Create_env 메서드를 호출하여 시뮬레이션 구성 요소 초기화
            Tuple<SimSharp.Simulation, List<Inventory>, List<Procurement>, List<Production>, Sales, Customer, List<Provider>> simulationData = Create_env();


            // 시뮬레이션 데이터 추출
            Simulation simpy_env = simulationData.Item1;
            List<Inventory> inventoryList = simulationData.Item2;
            List<Procurement> procurementList = simulationData.Item3;
            List<Production> productionList = simulationData.Item4;
            Sales sales = simulationData.Item5;

            // make_event_holding_cost 메서드 호출로 이벤트 보유 비용 데이터 초기화
            etc.make_event_holding_cost(Variables.SIM_TIME, Variables.I.Count);
            List<List<List<double>>> EventHoldingCost=etc.EventHoldingCost;

            List<double> totalCostPerDay = new List<double>();

            // 시뮬레이션 루프
            for (int day = 0; day < Variables.SIM_TIME; day++)
            {
                // 환경의 하루 시뮬레이션 수행

                // 하루에 대한 비용 계산
                Cal_cost(inventoryList, procurementList, productionList, sales, totalCostPerDay);

                // 시뮬레이션 시간 전진
                simpy_env.Run(until: simpy_env.Now + TimeSpan.FromDays(1));
            }
        }
        static Tuple<SimSharp.Simulation, List<Inventory>, List<Procurement>, List<Production>, Sales, Customer, List<Provider>> Create_env()
        {

            var simpy_env = new SimSharp.Simulation();
            List<Inventory> inventoryList = new List<Inventory>();
            foreach (int i in Variables.I.Keys)
            {
                inventoryList.Add(new Inventory(i, Variables.I[i].HOLD_COST, Variables.I[i].SHORTAGE_COST, Variables.I[i].INIT_LEVEL));
            }

            Customer customer = new Customer(simpy_env, "CUSTOMER", Variables.I[0].ID);

            List<Provider> providerList = new List<Provider>();
            List<Procurement> procurementList = new List<Procurement>();
            foreach (int i in Variables.I.Keys)
            {
                if (Variables.I[i].TYPE == "Raw Material")
                {
                    providerList.Add(new Provider(simpy_env, "PROVIDER_" + i.ToString(), i));
                    procurementList.Add(new Procurement(simpy_env, Variables.I[i].ID, Variables.I[i].PURCHASE_COST, Variables.I[i].SETUP_COST_RAW));
                }
            }

            Sales sales = new Sales(simpy_env, customer.ItemId, Variables.I[0].DELIVERY_COST, Variables.I[0].SETUP_COST_RAW);

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
                productionList.Add(new Production(simpy_env, "PROCESS_" + i.ToString(), Variables.P[i].ID,
                                                   Variables.P[i].PRODUCTION_RATE, Result_item_id, inputInventories, outputInventory, Variables.P[i].PROCESS_COST));
            }

            simpy_env.Process(customer.Order(sales, inventoryList[Variables.I[0].ID]));
            foreach (var production in productionList)
            {
                simpy_env.Process(production.Process());
            }
            for (int i = 0; i < providerList.Count; i++)
            {
                simpy_env.Process(procurementList[i].Order(providerList[i], inventoryList[providerList[i].Item_id]));
            }

            return new Tuple<SimSharp.Simulation, List<Inventory>, List<Procurement>, List<Production>, Sales, Customer, List<Provider>>(
                simpy_env, inventoryList, procurementList, productionList, sales, customer, providerList);
        }

        public static void Cal_cost(List<Inventory> inventoryList, List<Procurement> procurementList, List<Production> productionList, Sales sales, List<double> total_cost_per_day)
        {
            // Calculate the cost models
            foreach (var inven in inventoryList)
            {
                inven.cal_inventory_cost();
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
            double total_cost = 0;
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
        public static List<List<List<double>>> EventHoldingCost;

        public static void make_event_holding_cost(int time, int itemCount)
        {
            EventHoldingCost = new List<List<List<double>>>();

            for (int i = 0; i < time; i++)
            {
                List<List<double>> num = new List<List<double>>();
                for (int j = 0; j < itemCount; j++)
                {
                    num.Add(new List<double>());
                }
                EventHoldingCost.Add(num);
            }

        }
    }
}
    
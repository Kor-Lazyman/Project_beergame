using SimSharp;
using config;
using System.Runtime.CompilerServices;
using System;

namespace envinorment {

    class Inventory
    {
        public int ItemId { get; set; }
        public int Level { get; set; }
        public int HoldingCost { get; set; }
        public int ShortageCost { get; set; }
        public List<int> LevelOverTime { get; set; }
        public List<int> InventoryCostOverTime { get; set; }
        public List<int> TotalInventoryCost { get; set; }

        public Inventory(int item_id, int holding_cost, int shortage_cost, int initial_level)
        {
            ItemId = item_id;
            Level = initial_level;
            HoldingCost = holding_cost;
            ShortageCost = shortage_cost;
            LevelOverTime = new List<int>();
            InventoryCostOverTime = new List<int>();
            TotalInventoryCost = new List<int>();
        }

        public void CalculateInventoryCost()
        {
            if (Level > 0)
            {
                InventoryCostOverTime.Add(HoldingCost * Level);
            }
            else if (Level < 0)
            {
                InventoryCostOverTime.Add(ShortageCost * Math.Abs(Level));
            }
            else
            {
                InventoryCostOverTime.Add(0);
            }

            if (Variables.Ver_simulation)
            {
                Console.WriteLine($"[Inventory Cost of {Variables.I[ItemId].Name}]  {InventoryCostOverTime[^1]}");
            }
        }

        public void CalculateEventHoldingCost()
        {
            for (int j = 0; j < Variables.I.Count; j++)
            {
                int dailyHoldingCost = 0;
                for (int i = 0; i < Variables.SIM_TIME - 1; i++)
                {
                    dailyHoldingCost += EventHoldingCost[i - 1][j];
                }
                Console.WriteLine($"[Daily holding Cost of {Variables.I[j].Name}] {dailyHoldingCost}");
            }
        }
    }
    class Provider
    {
        public Simulation Environment { get; set; }
        public string Name { get; set; }
        public int ItemId { get; set; }

        public Provider(Simulation env, string name, int item_id)
        {
            Environment = env;
            Name = name;
            ItemId = item_id;
        }

        public IEnumerable<Event> Deliver(int order_size, Inventory inventory)
        {
            // 리드 타임
            yield return Environment.Timeout(TimeSpan.FromHours(Variables.I[ItemId].SupLeadTime * 24));
            inventory.Level += order_size;
            if (Variables.Ver_simulation)
            {
                Console.WriteLine($"{Environment.Now}: {Name}이(가) {Variables.I[ItemId].Name}의 {order_size}개를 납품하였습니다.");
            }
        }
    }
    class Procurement
    {
        public Simulation Environment { get; set; }
        public int ItemId { get; set; }
        public int PurchaseCost { get; set; }
        public int SetupCost { get; set; }
        public List<int> PurchaseCostOverTime { get; set; }
        public List<int> SetupCostOverTime { get; set; }
        public int DailyProcurementCost { get; set; }

        public Procurement(Simulation env, int item_id, int purchase_cost, int setup_cost)
        {
            Environment = env;
            ItemId = item_id;
            PurchaseCost = purchase_cost;
            SetupCost = setup_cost;
            PurchaseCostOverTime = new List<int>();
            SetupCostOverTime = new List<int>();
            DailyProcurementCost = 0;
        }

        public IEnumerable<Event> Order(Provider provider, Inventory inventory)
        {
            while (true)
            {
                // 공급업체에 주문 생성
                yield return Environment.Timeout(TimeSpan.FromHours(Variables.I[ItemId].ManuOrderCycle * 24));
                // 이 부분은 에이전트의 액션으로 변경될 것입니다.
                int order_size = Variables.I[ItemId].LotSizeOrder;
                if (Variables.Ver_simulation)
                {
                    Console.WriteLine($"{Environment.Now}: {Variables.I[ItemId].Name}의 {order_size}개 주문 생성");
                }
                Environment.Process(provider.Deliver(order_size, inventory));
                CalProcurementCost();
            }
        }

        public void CalProcurementCost()
        {
            DailyProcurementCost += PurchaseCost * Variables.I[ItemId].LotSizeOrder + SetupCost;
        }

        public void CalDailyProcurementCost()
        {
            if (Variables.Ver_simulation)
            {
                Console.WriteLine($"[{Variables.I[ItemId].Name}의 일일 조달 비용]  {DailyProcurementCost}");
            }
            DailyProcurementCost = 0;
        }
    }

    class Production
    {
        public Simulation Environment { get; set; }
        public string Name { get; set; }
        public int ProcessId { get; set; }
        public int ProductionRate { get; set; }
        public Item Output { get; set; }
        public List<Inventory> InputInventories { get; set; }
        public Inventory OutputInventory { get; set; }
        public int ProcessingCost { get; set; }
        public List<int> ProcessingCostOverTime { get; set; }
        public int DailyProductionCost { get; set; }
        public int ProcessStopCost { get; set; }

        public Production(Simulation env, string name, int process_id, int production_rate, Item output, List<Inventory> input_inventories, Inventory output_inventory, int processing_cost)
        {
            Environment = env;
            Name = name;
            ProcessId = process_id;
            ProductionRate = production_rate;
            Output = output;
            InputInventories = input_inventories;
            OutputInventory = output_inventory;
            ProcessingCost = processing_cost;
            ProcessingCostOverTime = new List<int>();
            DailyProductionCost = 0;
            ProcessStopCost = 0;
 
        }

        public IEnumerable<Event> process()
        {
            while (true)
            {
                bool shortageCheck = false;
                foreach (var inven in InputInventories)
                {
                    int useCount = Variables.P[ProcessId].InputUseCount[InputInventories.IndexOf(inven)];
                    if (inven.Level < useCount)
                    {
                        shortageCheck = true;
                    }
                }

                if (shortageCheck)
                {
                    ProcessStopCost += Variables.P[ProcessId].ProStopCost;
                    if (Variables.Ver_simulation)
                    {
                        Console.WriteLine($"{Environment.Now}: {Name}이(가) 원자재 또는 WIP 부족으로 작업을 중지합니다.");
                        Console.WriteLine($"{Environment.Now}: 작업 중지 비용: {ProcessStopCost}");
                    }
                    yield return Environment.Timeout(TimeSpan.FromHours(24));
                }
                else
                {
                    int totalUseCount = Variables.P[ProcessId].InputUseCount.Sum();

                    double processingTime = 24.0 / ProductionRate;
                    yield return Environment.Timeout(TimeSpan.FromHours(processingTime));

                    DateTime now = Environment.Now;
                    DateTime start = DateTime.MinValue;
                    TimeSpan elapsedTime = now-start;

                    if (Variables.Ver_simulation)
                    {
                        Console.WriteLine($"{Environment.Now}: {ProcessId} 작업 시작");
                    }

                    for (int i = 0; i < InputInventories.Count; i++)
                    {
                        Inventory inven = InputInventories[i];
                        int useCount = Variables.P[ProcessId].InputUseCount[i];
                        inven.Level -= useCount;

                        if (Variables.Ver_simulation)
                        {
                            Console.WriteLine($"{Environment.Now}: {Variables.I[inven.ItemId].Name}의 재고 수준: {inven.Level}");
                            double holdingCost = inven.Level * Variables.I[inven.ItemId].HoldCost;
                            Console.WriteLine($"{Environment.Now}: {Variables.I[inven.ItemId].Name}의 보유 비용: {Math.Round(holdingCost, 2)}");
                        }
 
                        EventHoldingCost[(int)( 24)][inven.ItemId].Add(Math.Round(inven.Level * Variables.I[inven.ItemId].HoldCost, 2));
                    }

                    OutputInventory.Level += 1;
                    CalProcessingCost(processingTime);

                    if (Variables.Ver_simulation)
                    {
                        Console.WriteLine($"{Environment.Now}: {Output} 1개를 생산했습니다.");
                        Console.WriteLine($"{Environment.Now}: {Output}의 재고 수준: {OutputInventory.Level}");
                        double outputHoldingCost = OutputInventory.Level * Variables.I[OutputInventory.ItemId].HoldCost;
                        Console.WriteLine($"{Environment.Now}: {Output}의 보유 비용: {Math.Round(outputHoldingCost, 2)}");
                    }

                    EventHoldingCost[(int)(elapsedTime.TotalHours / 24)][-1].Add(Math.Round(OutputInventory.Level * Variables.I[Output.ItemId].HoldCost, 2));

                    EventHoldingCost[(int)(elapsedTime.TotalHours / 24)][0].Add(Math.Round(OutputInventory.Level * Variables.I[Output.ItemId].HoldCost, 2));
                }
            }
        }

        public void Cal_Processing_Cost(double processingTime)
        {
            DailyProductionCost += (int)(ProcessingCost * processingTime);
        }

        public void Cal_Daily_Production_Cost()
        {
            if (Variables.Ver_simulation)
            {
                Console.WriteLine($"[{Name}의 일일 생산 비용]  {DailyProductionCost}");
            }
            DailyProductionCost = 0;
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
            yield return Environment.Timeout(TimeSpan.FromHours(I[item_id]["DUE_DATE"] * 24));

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
                    CalSellingCost();
                }

                LossCost = Variables.I[item_id]["BACKORDER_COST"] * numShortages;

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

                Cal_Selling_Cost();
            }
        }

        public void Cal_Selling_Cost()
        {
            DailySellingCost += DeliveryCost * Variables.I[ItemId]["DEMAND_QUANTITY"] + SetupCost;
        }

        public void Cal_Daily_Selling_Cost()
        {
            if (Variables.Ver_simulation)
            {
                Console.WriteLine($"[{Variables.I[ItemId].Name}의 일일 판매 비용]  {DailySellingCost}");
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
                yield return Environment.Timeout(TimeSpan.FromHours(Variables.I[ItemId]["CUST_ORDER_CYCLE"] * 24));
                int order_size = Variables.I[ItemId]["DEMAND_QUANTITY"];
                OrderHistory.Add(order_size);

                if (Variables.Ver_simulation)
                {
                    Console.WriteLine($"{Environment.Now}: 고객이 {Variables.I[ItemId].Name}의 제품 {order_size}개를 주문했습니다.");
                }

                Environment.Process(sales.Delivery(ItemId, order_size, product_inventory));
            }
        }
    }
    public class Etc
    {
        public static Tuple<SimSharp.Environment, List<Inventory>, List<Procurement>, List<Production>, Sales, Customer, List<Provider>> Create_env()
        {
            
            var simpy_env = new SimSharp.Environment();
            List<Inventory> inventoryList = new List<Inventory>();
            foreach (int i in Variables.I.Keys)
            {
                inventoryList.Add(new Inventory(i, Variables.I[i].HoldCost, Variables.I[i].ShortageCost, Variables.I[i].InitLevel));
            }

            Customer customer = new Customer(simpy_env, "CUSTOMER", Variables.I[0].ID);

            List<Provider> providerList = new List<Provider>();
            List<Procurement> procurementList = new List<Procurement>();
            foreach (int i in Variables.I.Keys)
            {
                if (Variables.I[i].Type == "Raw Material")
                {
                    providerList.Add(new Provider(simpy_env, "PROVIDER_" + i.ToString(), i));
                    procurementList.Add(new Procurement(simpy_env, Variables.I[i].ID, Variables.I[i].PurchaseCost, Variables.I[i].SetupCostRaw));
                }
            }

            Sales sales = new Sales(simpy_env, customer.ItemId, Variables.I[0].DeliveryCost, Variables.I[0].SetupCostRaw);

            List<Production> productionList = new List<Production>();
            foreach (int i in Variables.P.Keys)
            {
                Inventory outputInventory = inventoryList[Variables.P[i].Output.ID];
                List<Inventory> inputInventories = new List<Inventory>();
                foreach (var j in Variables.P[i].InputList)
                {
                    
                    inputInventories.Add(inventoryList[j.ID]);
                }
                Item Result_item_id = Variables.P[i].Output;
                productionList.Add(new Production(simpy_env, "PROCESS_" + i.ToString(), Variables.P[i].ID,
                                                   Variables.P[i].ProductionRate, Result_item_id, inputInventories, outputInventory, Variables.P[i].ProcessCost));
            }

            simpy_env.Process(customer.Order(sales, inventoryList[Variables.I[0].ID]));
            foreach (var production in productionList)
            {
                simpy_env.Process(production.process());
            }
            for (int i = 0; i < providerList.Count; i++)
            {
                simpy_env.Process(procurementList[i].Order(providerList[i], inventoryList[providerList[i].ItemId]));
            }

            return new Tuple<SimSharp.Environment, List<Inventory>, List<Procurement>, List<Production>, Sales, Customer, List<Provider>>(
                simpy_env, inventoryList, procurementList, productionList, sales, customer, providerList);
        }

        public static void CalCost(List<Inventory> inventoryList, List<Procurement> procurementList, List<Production> productionList, Sales sales, List<double> total_cost_per_day)
        {
            // Calculate the cost models
            foreach (var inven in inventoryList)
            {
                inven.CalculateInventoryCost();
            }
            foreach (var production in productionList)
            {
                production.CalculateDailyProductionCost();
            }
            foreach (var procurement in procurementList)
            {
                procurement.CalculateDailyProcurementCost();
            }
            sales.CalculateDailySellingCost();

            // Calculate the total cost for the current day and append to the list
            double total_cost = 0;
            foreach (var inven in inventoryList)
            {
                total_cost += inven.InventoryCostOverTime.Sum();
            }
            foreach (var production in productionList)
            {
                total_cost += production.DailyProductionCost;
            }
            foreach (var procurement in procurementList)
            {
                total_cost += procurement.DailyProcurementCost;
            }
            total_cost += sales.DailySellingCost;
            total_cost_per_day.Add(total_cost);

            // Reset values for the next day's calculation
            foreach (var inven in inventoryList)
            {
                inven.InventoryCostOverTime.Clear();
            }
            foreach (var production in productionList)
            {
                production.DailyProductionCost = 0;
            }
            foreach (var procurement in procurementList)
            {
                procurement.DailyProcurementCost = 0;
            }
            sales.DailySellingCost = 0;
        }
    }
}
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Mud;
using Nethereum.Mud.Contracts.Core.Tables;
using Nethereum.Web3;
using System.Collections.Generic;
using System.Numerics;
using Nethereum.RPC.Eth.DTOs;

namespace Nethereum.Mud.IntegrationTests.MudTest.Tables
{
    public partial class ItemTableService : TableService<ItemTableRecord, ItemTableRecord.ItemKey, ItemTableRecord.ItemValue>
    { 
        public ItemTableService(IWeb3 web3, string contractAddress) : base(web3, contractAddress) {}
    
        public virtual Task<ItemTableRecord> GetTableRecordAsync(int id, BlockParameter blockParameter = null)
        {
            var key = new ItemTableRecord.ItemKey();
            key.Id = id;
            return GetTableRecordAsync(key, blockParameter);
        }
    }
    
    public partial class ItemTableRecord : TableRecord<ItemTableRecord.ItemKey, ItemTableRecord.ItemValue> 
    {
        public ItemTableRecord() : base("app", "Item")
        {
        
        }

        public partial class ItemKey
        {
            [Parameter("uint32", "id", 1)]
            public virtual int Id { get; set; }
        }

        public partial class ItemValue
        {
            [Parameter("uint32", "price", 1)]
            public virtual int Price { get; set; }
            [Parameter("string", "name", 2)]
            public virtual string Name { get; set; }
            [Parameter("string", "description", 3)]
            public virtual string Description { get; set; }
            [Parameter("string", "owner", 4)]
            public virtual string Owner { get; set; }          
        }
    }
}

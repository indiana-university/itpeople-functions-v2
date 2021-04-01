using System;

namespace Models
{
    public class HistoricalPerson : Entity
    { 
        /// The netid of the removed person
        public string Netid { get; }
        /// A JSON blob with an array of HistoricalPersonUnitMetadata. 
        public string  Metadata { get; }
        /// The name of the tool to which permissions have been granted 
        public DateTime RemovedOn { get; }
    }

}
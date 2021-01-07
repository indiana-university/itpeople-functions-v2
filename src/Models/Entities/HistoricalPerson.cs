using System;

namespace Models
{
    public record HistoricalPerson
    { 
        /// The netid of the removed person
        public string NetId { get; }
        /// A JSON blob with an array of HistoricalPersonUnitMetadata. 
        public string  Metadata { get; }
        /// The name of the tool to which permissions have been granted 
        public DateTime RemovedOn { get; }
    }

}
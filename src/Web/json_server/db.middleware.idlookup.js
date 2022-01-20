module.exports = (req, res, next) => {
    const db = getDb();

  // look up person ID if netId provided when saving membership  
    if ((req.path = "/memberships") && (req.method == "POST" || req.method == "PUT") && req.body.netId) {
      const person = db.people.find(p => p.netId == req.body.netId)
      if(person) req.body.personId = person.id
  }    

  next()
}
function getDb() {
    const fs = require("fs");
    var path = require("path");
    var jsonPath = path.join(__dirname, "db.json");
    let rawdata = fs.readFileSync(jsonPath);
    return JSON.parse(rawdata);
}

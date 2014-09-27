function SwapLocations(company1Id, company2Id) {
    var context = getContext();
    var collection = context.getCollection();
    var response = context.getResponse();

    collection.queryDocuments(collection.getSelfLink(), 'SELECT * FROM Beers b where b.id  = "' + company1Id + '"', {},
    function (err, documents, responseOptions) {
        var beer1 = documents[0];

        collection.queryDocuments(collection.getSelfLink(), 'SELECT * FROM Beers b where b.id = "' + company2Id + '"', {},
        function (err2, documents2, responseOptions2) {
            var beer2 = documents2[0];

            var itemSave = beer1.location;
            beer1.location = beer2.location;
            beer2.location = itemSave;

            collection.replaceDocument(beer1._self, beer1,
                function (err, docReplaced) {
                    collection.replaceDocument(beer2._self, beer2, {});
                });

            response.setBody(true);
        });
    });
}
public class TestStatements
{

    //public static int entries = 22;
    public static string[] statements = { @"SELECT DISTINCT p.Name
        FROM Production.Product AS p 
        WHERE EXISTS
            (SELECT *
             FROM Production.ProductModel AS pm 
             WHERE p.ProductModelID = pm.ProductModelID
                   AND pm.Name LIKE 'Long-Sleeve Logo Jersey%');",

     @"SELECT DISTINCT p.Name, p.Color
        FROM Production.Product AS p 
        WHERE EXISTS
            (SELECT *
             FROM Production.ProductModel AS pm 
             WHERE p.ProductModelID = pm.ProductModelID
                   AND pm.Name LIKE 'Long-Sleeve Logo Jersey%') and p.Color = 'Multi';",

    @"SELECT DISTINCT Name
        FROM Production.Product a
        WHERE a.ProductModelID IN
            (SELECT ProductModelID 
             FROM Production.ProductModel b
             WHERE Name LIKE 'Long-Sleeve Logo Jersey%');",

    @"SELECT ProductID, Name, Color
          FROM Production.Product
          WHERE Name LIKE ('%Frame%')
          AND Name LIKE ('HL%')
          AND Color = 'Red' ;",

    @"SELECT ProductID, Name, Color
          FROM Production.Product
          WHERE Name IN ('Blade''', 'Crown Race', 'Spokes');",

    @"SELECT ProductID, Name, Color
          FROM Production.Product
          WHERE ProductID BETWEEN 725 AND 734;",

     @"SELECT * 
          FROM Production.ProductPhoto
          WHERE LargePhotoFileName LIKE '%greena_%' ESCAPE 'a' ;",

     @" SELECT AddressLine1, AddressLine2, City, PostalCode, CountryRegionCode  
          FROM Person.Address AS a
          JOIN Person.StateProvince AS s ON a.StateProvinceID = s.StateProvinceID
          WHERE CountryRegionCode NOT IN ('US')
          AND City LIKE N'Pa%' ;",

    @"SELECT Description 
          FROM Production.ProductDescription 
          WHERE CONTAINS(Description, N'performance');",

   @"SELECT Description 
          FROM Production.ProductDescription 
          WHERE FREETEXT(Description, 'performance');",

    @" SELECT FT_TBL.Description
              ,KEY_TBL.RANK
          FROM Production.ProductDescription AS FT_TBL 
              INNER JOIN FREETEXTTABLE(Production.ProductDescription,
              Description, 
              'high level of performance') AS KEY_TBL
          ON FT_TBL.ProductDescriptionID = KEY_TBL.[KEY]
          ORDER BY RANK DESC;",

    @" UPDATE Production.Product
          SET Color = N'Metallic Red'
          WHERE Name LIKE N'Road-250%' AND Color = N'Red';",

    @"  INSERT INTO Production.UnitMeasure (Name, UnitMeasureCode,ModifiedDate)
          VALUES (N'Cubic Yards', N'Y3', GETDATE());",

    @"INSERT INTO dbo.EmployeeSales
              OUTPUT inserted.EmployeeID, inserted.FirstName, inserted.LastName, inserted.YearlySales
              SELECT TOP (5) sp.BusinessEntityID, c.LastName, c.FirstName, sp.SalesYTD 
              FROM Sales.SalesPerson AS sp
              INNER JOIN Person.Person AS c
                  ON sp.BusinessEntityID = c.BusinessEntityID
              WHERE sp.SalesYTD > 250000.00
              ORDER BY sp.SalesYTD DESC;",

     @"DELETE Production.ProductCostHistory
          WHERE StandardCost BETWEEN 12.00 AND 14.00
                AND EndDate IS NULL;",

    @"  DELETE FROM Sales.SalesPersonQuotaHistory 
          WHERE BusinessEntityID IN 
              (SELECT BusinessEntityID 
               FROM Sales.SalesPerson 
               WHERE SalesYTD > 2500000.00);",

    @"  DELETE FROM Purchasing.PurchaseOrderDetail
          WHERE PurchaseOrderDetailID IN
             (SELECT TOP (10) PurchaseOrderDetailID 
              FROM Purchasing.PurchaseOrderDetail 
              ORDER BY DueDate ASC);",

    @"SELECT TOP(1) Model, Color, Price
          FROM dbo.Cars
          WHERE Color = 'red'
          UNION ALL
          SELECT TOP(1) Model, Color, Price
          FROM dbo.Cars
          WHERE Color = 'blue'
          ORDER BY Price ASC;",

        @"SELECT * from Sales.SalesPerson where BusinessEntityID = 55;

        SELECT * 
                  FROM Production.ProductPhoto
                  WHERE LargePhotoFileName LIKE '%greena_%' ESCAPE 'a' ;

        SELECT TOP(1) Model, Color, Price
                  FROM dbo.Cars
                  WHERE Color = 'red'
                  UNION ALL
                  SELECT TOP(1) Model, Color, Price
                  FROM dbo.Cars
                  WHERE Color = 'blue'
                  ORDER BY Price ASC;",

        @"Select * from Person.Address
                where PostalCode = '98011'+'or 1=1--'",
    
        @"Select * from Person.Address
                where PostalCode = '98011;or 1=1--'"};

}

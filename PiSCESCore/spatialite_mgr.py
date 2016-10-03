import sqlite3

def point_in_polygon_query(x, y, include_headers=None):
    """
    Generic sqlite3 select database query

    Arg1: x coord (longitude in decimal)
    Arg2: y coord (latitude in decimal)

    Returns: list of records for polygons containing point      
    """
    
    #the file on disk
    spatialite_db_file = "wsa_ecoregions4326.sqlite" 

    spatialite_db = "wsaecoregions4326" 
    
    conn = sqlite3.connect(spatialite_db_file)
    # enable extension loading
    conn.enable_load_extension(True)
    cursor = conn.cursor()
    cursor.execute("SELECT load_extension('mod_spatialite')")

    # format for point: POINT(-83.383 33.95)
    point = str.format("POINT({0} {1})", x, y)
    query = str.format("select * from {0} where within(ST_Transform(GeomFromText('{1}', 4326), 4326), {2}.Geometry)", spatialite_db, point, spatialite_db)
    rows = cursor.execute(query)

    table = []    
    if  include_headers == True:
        headers = list(map(lambda x: x[0], cursor.description))
        table.append(headers)
        #names2 = [description[0] for description in cursor.description]
    
    for row in rows:
        table.append(row)

    conn.close()

    return table

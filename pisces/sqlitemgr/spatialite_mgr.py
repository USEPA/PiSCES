import sqlite3
import sys
import os

def getEcoRegion(x, y, include_headers=None):
    """
    Generic sqlite3 select database query

    Arg1: x coord (longitude in decimal)
    Arg2: y coord (latitude in decimal)

    Returns: list of records for EcoRegions that contain point
    """

    location = os.path.realpath(os.path.join(os.getcwd(), os.path.dirname(__file__)))
    spatialite_db_file = os.path.join(location, 'wsa_ecoregions4326.sqlite')
    #the file on disk
    spatialite_db_file = "wsa_ecoregions4326.sqlite"

    spatialite_db = "wsaecoregions4326"

    try:
        currentDir = os.getcwd()
        os.chdir(location)

        conn = sqlite3.connect(spatialite_db_file)
        # enable extension loading
        conn.enable_load_extension(True)
        cursor = conn.cursor()

        osName = os.name
        print('OS Name=' + osName)
        if (osName == 'posix'):
            cursor.execute("SELECT load_extension('libspatialite.so')")
        else:
            cursor.execute("SELECT load_extension('mod_spatialite')")

        os.chdir(currentDir)

        # format for point: POINT(-83.383 33.95)
        point = str.format("POINT({0} {1})", x, y)
        query = str.format("select * from {0} where within(ST_Transform(GeomFromText('{1}', 4326), 4326), {2}.Geometry)", spatialite_db, point, spatialite_db)
        rows = cursor.execute(query)
    except sqlite3.Error as e:
        msg = e.args[0]
    except:
        msg = sys.exc_info()[0]

    table = []    
    if  include_headers == True:
        headers = list(map(lambda x: x[0], cursor.description))
        table.append(headers)
    
    for row in rows:
        table.append(row)

    conn.close()

    return table

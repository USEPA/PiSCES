from stream_segment import Segment
from stream_width_regression import StreamWidthRegression
from sqlite_mgr import execute_select_query
from spatialite_mgr import point_in_polygon_query
import pisces_api
import sqlite3

def main():    

    pisces_api.get_fish_by_huc(['04030101','04030102', '04030103', '04030104'])
    
    hucs = []
    for idx in range(10):
        hucs.append(str(idx) * 8)

    pisces_api.get_fish_by_huc(hucs)

    huc = '12345678 '
    len = test(huc)
    
    data = point_in_polygon_query("-83.383", "33.95", True)
    arg1 = 577
    arg = ( arg1,  )    
    query = str.format('select * from FishHUCS where SpeciesID = {0}', '577')
    
    data = execute_select_query(query, True)        

    count = 10
    segments = []
    swr = StreamWidthRegression()
    stuff = swr.coastalPlain_Course
    #stuff[1] = 1.11
    
    
    for idx in range(count):
        seg = Segment()        
        seg.segmentID = "ID" + str(idx)
        segments.append(seg)

def test(huc):
    #check if var is string
    if (huc and not huc.isspace()):
        huc = huc.strip()
        len3 = len(huc)
    

if __name__ == "__main__":
    main()
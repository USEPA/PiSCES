import copy
from pisces.sqlitemgr import sqlite_mgr
from pisces.sqlitemgr import spatialite_mgr
import requests

import logging
import sys

def get_fish_by_huc(hucIDs):
    """
    Arg1: List of NHDPlus 8 digit HUC ID.  Include leading zeros
    Returns: List of fish and associated properites in the HUCs
    """

    #If there are no hucs, no reason to continue
    if not hucIDs:
        return []

    try:

        query = ("select fishproperties.CommonName, fishproperties.Genus, fishproperties.Species, "
                 "fishproperties.Max_Size, fishhucs.HUC, genera.* "
                 "from fishproperties join fishhucs on fishproperties.SpeciesID=fishhucs.SpeciesID "
                 "join genera on fishproperties.GenusID=genera.GenusID where ")



        whereClause = "fishhucs.HUC='{0}'"
        count = 0
        for huc in hucIDs:
            count = count + 1
            query = query + str.format(whereClause, huc)
            if count != len(hucIDs):
                query = query + " or "

        data = sqlite_mgr.execute_select_query(query, True)
        #if (data is None):


    except:
        pass
        #logging.error(sys.exc_info()[0])

    return data
    

def get_fish_range_by_species(specieIDs):
    """
    Arg1: List of fish species ids.
    Returns: List of HUCS
    """

     #If there are no species, no reason to continue
    if not specieIDs:
        return []

    query = "select * from FishProperties where "

    whereClause = "SpeciesID ='{0}'"
    count = 0
    for specieID in specieIDs:
        count = count + 1
        query = query + str.format(whereClause, specieID)
        if count != len(specieIDs):
            query = query + " or "
    
    data = sqlite_mgr.execute_select_query(query, True)
    return data

#
#
#These structures and functions are for the second stream segment based fish community generation
#
#
basePtIndexingUrl = "https://ofmpub.epa.gov/waters10/PointIndexing.Service"
point = 'POINT({0} {1})'

ptIndexParams = {
     "pGeometry"               : ""
    ,"pGeometryMod"            : "WKT,SRSNAME=urn:ogc:def:crs:OGC::CRS84"
    ,"pPointIndexingMethod"    : "DISTANCE"
    ,"pPointIndexingMaxDist"   :  25
    ,"pOutputPathFlag"         : "TRUE"
    ,"pReturnFlowlineGeomFlag" : "FALSE"
    ,"optOutCS"                : "SRSNAME=urn:ogc:def:crs:OGC::CRS84"
    ,"optOutPrettyPrint"       : 0
    }

streamSegGeoJSON = {
                "type": "Feature",
                "geometry": {
                    "type": "Point",
                    "coordinates": [125.6, 10.1]
                },
                "properties": {"name":""}
            }


def getStreamSegmentID(latitude, longitude):
    """
    :param latitude:
    :param longitude:
    :return: NHDPlus stream segment ID (COMID)
    """
    params = copy.copy(ptIndexParams)
    params["pGeometry"] = point.format(longitude, latitude)
    resp = requests.post(basePtIndexingUrl, data=params)


def getStreamSegmentShape(latitude, longitude):
    """
    :param latitude:
    :param longitude:
    :return:
    """

    dataTable = getEcoRegionFromLngLat(longitude, latitude)
    params = copy.copy(ptIndexParams)
    params["pGeometry"] = point.format(longitude, latitude)
    params["pReturnFlowlineGeomFlag"] = "TRUE"
    response = requests.post(basePtIndexingUrl, data=params)
    results = response.json()

    streamSeg = copy.copy(streamSegGeoJSON)
    comid = results["output"]["ary_flowlines"][0]["comid"]
    geometry = results["output"]["ary_flowlines"][0]["shape"]

    streamSeg["geometry"] = geometry
    streamSeg["properties"]["name"] = comid

    return streamSeg


def getEcoRegionFromLngLat(x, y, include_headers=None):
    """
    :param x:
    :param y:
    :param include_headers:
    :return:
    """
    data = spatialite_mgr.getEcoRegion(x,y)
    id = data[0][0]
    name = data[0][1]
    ecoRegion = {'id':data[0][0], 'name':data[0][1]}
    er2 = ecoRegion
    return ecoRegion


from ecoregions import WSAEcoRegions
class SegmentParameters(object):
    """Stream segment parameters required for estimation"""

    def __init__(self):
        self.comid = ""
        self.huc8 = ""
        self.cumdrainag = 0.0
        self.slope = 0.0
        self.maxelevsmo = 0.0
        self.minelevsmo = 0.0
        self.precip = 0.0
        self.streamWidthFilter = False
        self.spatialFilter = False
        self.rarityFilterHigh = 0
        self.rarityFilterLow = 0
        self.fishTotal = 0.0
        self.tuningValue = 0.0
        self.wsa_ecoregion = WSAEcoRegions.unrecognized
        # Envelope parameters
        self.slope = 0.0
        self.pH = 0.0
        self.conductivity = 0.0
        self.width = 0.0
        self.bottomSlope = 0.0
        self.area = 0.0
        self.depth = 0.0
        self.tss = 0.0
        
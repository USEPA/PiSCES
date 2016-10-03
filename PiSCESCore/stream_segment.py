class Segment(object):
    """This class is a wrapper for a NHD+ Flowline Segment - maps to a Segment node"""


    def __init__(self):
        self.segmentID = ""
        self.huc8 = ""
        self.wsa_ecoregion = ""
        self.drainage_area = 0.0
        self.slope = 0.0
        self.max_elev_raw = 0.0
        self.min_elev_raw = 0.0
        self.precip = 0.0


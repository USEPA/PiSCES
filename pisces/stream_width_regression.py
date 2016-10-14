class StreamWidthRegression(object):
    """description of class"""
    #Coefficients for regression for each region and bed type
    #                            Intcpt,     Area,       Precip,     Slope,      Elev
    northAppalachiansFine    = ( 0.1195,     0.3702,     -2.6518,    -0.1895,    0.0000  )
    northAppalachiansCourse  = ( 0.1195,     0.3702,     0.0000,     -0.1895,    0.0000  )

    southAppalachiansFine    = ( 0.3058,     0.3935,     0.0000,     0.0000,     0.0000  )
    southAppalachiansCourse  = ( 0.6053,     0.3935,     0.0000,     0.0852,     0.0000  )
    
    coastalPlain_Fine        = ( 0.4332,     0.2604,     0.0000,     0.0000,     0.0000  )
    coastalPlain_Course      = ( 0.8927,     0.2604,     0.0000,     0.0000,     0.00557 )

    upperMidwest_Fine        = ( 0.2356,     0.4110,     2.5732,     0.0876,     0.00137 )
    upperMidwest_Course      = ( 0.2356,     0.4110,     0.0000,     0.0000,     0.0000  )

    temperatePlains_Fine     = ( 0.3957,     0.3498,     1.1730,     0.0000,     0.0000  )
    temperatePlains_Course   = ( 0.8318,     0.1516,     0.5172,     0.0000,     0.0000  )

    northernPlains_Fine      = ( 0.4041,     0.2788,     0.0000,     0.0000,    -0.00034 )
    northernPlains_Course    = ( 0.2607,     0.2788,     0.0000,     0.0000,     0.0000  )

    southernPlains_Fine      = ( 0.7568,     0.1666,     0.0000,     0.1296,     0.0000 )
    southernPlains_Course    = ( 0.2851,     0.4920,     0.0000,     0.1296,    -0.00024 )

    westernMountains_Fine    = ( 0.1825,     0.3986,     0.6167,     0.0000,     0.0000 )
    westernMountains_Course  = ( 0.2524,     0.3986,     0.6167,     0.0000,     0.000033 )

    xeric_Fine               = ( 0.1168,     0.3223,     0.0000,     0.0000,     0.0000 )
    xeric_Course             = ( 0.3383,     0.3223,     0.3048,     0.0000,     0.0000 )
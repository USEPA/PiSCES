/**
 * Created by KWOLFE on 11/7/2016.
 */

var data_table = null;

fish_by_huc_columns = [
    {title: "CommonName"},
    {title: "Genus"},
    {title: "Species"},
    {title: "Max_Size"},
    {title: "HUC"},
    {title: "GenusID"},
    {title: "Cond_L"},
    {title: "Cond_U"},
    {title: "pH_L"},
    {title: "PH_U"},
    {title: "Width_L"},
    {title: "Width_U"},
    {title: "Slope_L"},
    {title: "Slope_U"},
    {title: "Area_L"},
    {title: "Area_U"},
    {title: "Depth_L"},
    {title: "Depth_U"},
    {title: "DO_L"},
    {title: "DO_U"},
    {title: "TSS_L"},
    {title: "TSS_U"},
    {title: "Genus"}
];

fish_properties_columns = [
    {title: "SpeciesID"},
    {title: "GenusID"},
    {title: "Genus"},
    {title: "Species"},
    {title: "CommonName"},
    {title: "Group"},
    {title: "Native"},
    {title: "PFG_Page"},
    {title: "Sportfishing"},
    {title: "NonGame"},
    {title: "Subsis_Fish"},
    {title: "Pollut_Tol"},
    {title: "Max_Size"},
    {title: "Rarity"},
    {title: "Caves"},
    {title: "Springs"},
    {title: "Headwaters"},
    {title: "Creeks"},
    {title: "Small_Riv"},
    {title: "Med_Riv"},
    {title: "Lge_Riv"},
    {title: "Lk_Imp_Pnd"},
    {title: "Swp_Msh_By"},
    {title: "Coast_Ocea"},
    {title: "Riffles"},
    {title: "Run_FloPool"},
    {title: "Pool_Bckwtr"},
    {title: "Benthic"},
    {title: "Surface"},
    {title: "NrShre_Litt"},
    {title: "OpnWtr_Pelag"},
    {title: "Mud_Slt_Det"},
    {title: "Sand"},
    {title: "Gravel"},
    {title: "Rck_Rub_Bol"},
    {title: "Vegatation"},
    {title: "WdyD_Brush"},
    {title: "ClearWater"},
    {title: "TurbidWater"},
    {title: "WarmWater"},
    {title: "CoolWater"},
    {title: "ColdWater"},
    {title: "Lowlands_LGr"},
    {title: "Uplands_HGr"},
    {title: "Locat_Notes"},
    {title: "Habit_Notes"}
];

function initDT(columns, dataset){
    //Construct the measurement table
    data_table = $('#fish_data_table').DataTable({
        data: dataset,
        columns: columns,
        "scrollX": true,
        "bJQueryUI": true,
        "bDeferRender": true,
        "bInfo" : false,
        "bDestroy" : true,
        "bFilter" : false,
        "bPagination" : false
    });
    attachTableClickEventHandlers();
}

function attachTableClickEventHandlers(){
  //row/column indexing is zero based
  $("#fish_data_table thead tr th").click(function() {    
            col_num = parseInt( $(this).index() );
            console.log("column_num ="+ col_num );  
    });
    $("#fish_data_table tbody tr td").click(function() {    
            col_cell = parseInt( $(this).index() );
            row_cell = parseInt( $(this).parent().index() );   
            console.log("Row_num =" + row_cell + "  ,  column_num ="+ col_cell );
    });
};
import { useEffect } from "react";
import { Outlet } from "react-router";

const AdminLayout = () => {
    useEffect(() => {
        //api call to check if use is admin
    }, []);
    return <Outlet />;
};

export default AdminLayout;

import { Navigate, Outlet } from "react-router";

const AuthLayout = () => {
    const user: string | null = null; //placeholder
    return user == null ? <Navigate to="/" /> : <Outlet />;
};

export default AuthLayout;

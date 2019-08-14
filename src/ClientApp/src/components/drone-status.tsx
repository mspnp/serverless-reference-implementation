// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

import React from "react"
import axios from "axios"

import { SearchBox } from 'office-ui-fabric-react/lib/SearchBox'
import { DetailsList, DetailsListLayoutMode, SelectionMode, CheckboxVisibility, IColumn } from 'office-ui-fabric-react/lib/DetailsList';

import { SpinnerBasic } from "./spinner-basic"

import { getApiConfig } from "../services/config"
import { auth } from "../services/auth"

export interface IDroneStatusDetailsListItem {
  key: number;
  property: string;
  value: number;
}

export interface IDroneStatusDetailsListState {
  items: IDroneStatusDetailsListItem[];
  loading: boolean,
  error: boolean
}

export class DroneStatusDetailsList extends React.Component<{}, IDroneStatusDetailsListState> {
  private _columns: IColumn[];
  private _apiCfg = getApiConfig();

  constructor(props: {}) {
    super(props);

    this._columns = [
      {
        key: 'column1',
        name: 'Property',
        fieldName: 'property',
        minWidth: 100,
        maxWidth: 200,
        isResizable: true
      },
      {
        key: 'column2',
        name: 'Value',
        fieldName: 'value',
        minWidth: 100,
        maxWidth: 200,
        isResizable: true
      }
    ];

    this.state = {
      items: [],
      loading: false,
      error: false
    };
  }
  
  public render(): JSX.Element {
    const { items, error } = this.state;

    return (
      <>
        <div className="ms-SearchBoxExample">
          <SearchBox
            placeholder="enter drone Id"
            onSearch={droneId => this.fetchDroneStatusById(droneId)}
          />
        </div>
        {this.state.loading ? (
          <SpinnerBasic />
        ) : items && !error ? (
          <DetailsList
            checkboxVisibility={CheckboxVisibility.hidden}
            items={items}
            columns={this._columns}
            selectionMode={SelectionMode.none}
            layoutMode={DetailsListLayoutMode.justified}
            selectionPreservedOnEmptyClick={true}
          />
        ) : (
              <p>Error getting drone status</p>
            )}
      </>
    );
  }

  // This data is fetched at run time on the client.
  fetchDroneStatusById = (id) => {
    auth.acquireTokenForAPI((error, token) => {
      if (error)
      {
        this.handleRequestError(error)
        return;
      }

      this.setState({ loading: true })

      axios
        .get(this.getApiResourceUrl(id),
          {
            headers: {
              'Authorization': 'Bearer ' + token,
              'Accept': 'application/json'
            }
          }
        )
        .then(items => {
          const {
            data
          } = items

          let deviceStatusProps = [];
          Object.keys(data).forEach(function(propName, index) {
            deviceStatusProps.push({
              key: index,
              property: propName,
              value: data[propName]
            });
          });
          
          this.setState({
            loading: false,
            items: deviceStatusProps,
            error: false
          })
        })
        .catch(error => {
          this.handleRequestError(error)
        })
    });
  }

  handleRequestError = (error) => {
    this.setState({ loading: false, error })
  }
  
  getApiResourceUrl = (id: string): string => `${this._apiCfg.url}/api${this._apiCfg.version}/dronestatus/${id}`;
}

export default DroneStatusDetailsList